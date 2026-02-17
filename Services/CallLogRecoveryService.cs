using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Models.DTOs;
using TAB.Web.Models.Enums;

namespace TAB.Web.Services
{
    /// <summary>
    /// Service for handling call log recovery operations based on deadlines and verification status
    /// </summary>
    public class CallLogRecoveryService : ICallLogRecoveryService
    {
        private const string AutoOfficialCallTypePrefix = "Corporate Value Pack Data";

        private readonly ApplicationDbContext _context;
        private readonly ILogger<CallLogRecoveryService> _logger;
        private readonly INotificationService _notificationService;

        public CallLogRecoveryService(
            ApplicationDbContext context,
            ILogger<CallLogRecoveryService> logger,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Process all expired verification deadlines for a batch.
        /// Rule 1: Staff non-verification → All calls = PERSONAL
        /// </summary>
        public async Task<RecoveryResult> ProcessExpiredVerificationsAsync(Guid batchId)
        {
            try
            {
                _logger.LogInformation("Processing expired verifications for batch {BatchId}", batchId);

                var batch = await _context.StagingBatches
                    .FirstOrDefaultAsync(b => b.Id == batchId);

                if (batch == null)
                {
                    return RecoveryResult.CreateFailure("Batch not found");
                }

                var now = DateTime.UtcNow;

                // Get all TRULY unverified calls (staff did NOT verify or did NOT submit to supervisor)
                // This includes both initial verification failures AND re-verification failures (after revert)
                var submittedVerificationIds = await _context.CallLogVerifications
                    .Where(v => v.SubmittedToSupervisor && v.BatchId == batchId)
                    .Select(v => v.CallRecordId)
                    .ToListAsync();

                var unverifiedCalls = await _context.CallRecords
                    .Where(cr => cr.SourceBatchId == batchId
                              && cr.VerificationPeriod.HasValue
                              && cr.VerificationPeriod.Value < now  // Verification deadline has passed
                              && cr.IsVerified == false  // Staff did NOT verify
                              && !submittedVerificationIds.Contains(cr.Id)  // NOT submitted to supervisor
                              && (cr.AssignmentStatus == "None" || cr.AssignmentStatus == null)
                              && (cr.RecoveryStatus == "NotProcessed" || cr.RecoveryStatus == null))
                    .ToListAsync();

                if (unverifiedCalls.Count == 0)
                {
                    _logger.LogInformation("No unverified calls found for batch {BatchId}", batchId);
                    return RecoveryResult.CreateSuccess(0, 0, "No unverified calls to process");
                }

                var recoveryLogs = new List<RecoveryLog>();
                decimal totalRecovered = 0;

                // Group by staff member for tracking
                var callsByStaff = unverifiedCalls.GroupBy(c => c.ResponsibleIndexNumber ?? "Unknown");

                foreach (var staffGroup in callsByStaff)
                {
                    var staffIndex = staffGroup.Key;
                    var staffCalls = staffGroup.ToList();

                    foreach (var call in staffCalls)
                    {
                        // Skip auto-official calls — always treated as Official
                        if (call.CallType != null && call.CallType.StartsWith(AutoOfficialCallTypePrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            call.AssignmentStatus = "Official";
                            call.FinalAssignmentType = "Official";
                            call.RecoveryStatus = "Processed";
                            call.RecoveryDate = DateTime.UtcNow;
                            call.RecoveryProcessedBy = "System-AutoRecovery";
                            call.RecoveryAmount = 0;

                            recoveryLogs.Add(new RecoveryLog
                            {
                                CallRecordId = call.Id,
                                BatchId = call.SourceBatchId ?? Guid.Empty,
                                RecoveryType = "AutoOfficialExemption",
                                RecoveryAction = "Official",
                                RecoveryDate = DateTime.UtcNow,
                                RecoveryReason = $"Call type '{call.CallType}' is auto-official (Corporate Value Pack Data) and exempt from personal recovery.",
                                AmountRecovered = 0,
                                RecoveredFrom = staffIndex,
                                ProcessedBy = "System-AutoRecovery",
                                DeadlineDate = call.VerificationPeriod!.Value,
                                IsAutomated = true
                            });
                            continue;
                        }

                        call.AssignmentStatus = "Personal";
                        call.FinalAssignmentType = "Personal";
                        call.RecoveryStatus = "Processed";
                        call.RecoveryDate = DateTime.UtcNow;
                        call.RecoveryProcessedBy = "System-AutoRecovery";
                        call.RecoveryAmount = call.CallCost; // Use original currency amount

                        totalRecovered += call.RecoveryAmount ?? 0;

                        recoveryLogs.Add(new RecoveryLog
                        {
                            CallRecordId = call.Id,
                            BatchId = call.SourceBatchId ?? Guid.Empty,
                            RecoveryType = "StaffNonVerification",
                            RecoveryAction = "Personal",
                            RecoveryDate = DateTime.UtcNow,
                            RecoveryReason = $"Staff failed to verify call by deadline: {call.VerificationPeriod!.Value:yyyy-MM-dd HH:mm}. Call automatically recovered as personal.",
                            AmountRecovered = call.RecoveryAmount ?? 0,
                            RecoveredFrom = staffIndex,
                            ProcessedBy = "System-AutoRecovery",
                            DeadlineDate = call.VerificationPeriod!.Value,
                            IsAutomated = true
                        });
                    }

                    _logger.LogInformation(
                        "Processed {Count} unverified calls for staff {IndexNumber}, total amount: {Amount:C}",
                        staffCalls.Count,
                        staffIndex,
                        staffCalls.Sum(c => c.RecoveryAmount ?? 0));
                }

                // Save recovery logs
                if (recoveryLogs.Any())
                {
                    _context.RecoveryLogs.AddRange(recoveryLogs);

                    // Update batch totals
                    batch.TotalPersonalAmount = (batch.TotalPersonalAmount ?? 0) + totalRecovered;
                    batch.TotalRecoveredAmount = (batch.TotalRecoveredAmount ?? 0) + totalRecovered;

                    await _context.SaveChangesAsync();

                    // Send notifications to affected staff
                    // Use the earliest verification deadline for notification
                    var earliestDeadline = unverifiedCalls
                        .Where(c => c.VerificationPeriod.HasValue)
                        .Min(c => c.VerificationPeriod!.Value);
                    await NotifyStaffOfRecoveryAsync(callsByStaff.Select(g => g.Key).ToList(), "StaffNonVerification", earliestDeadline);
                }

                _logger.LogInformation(
                    "Successfully processed {Count} expired verifications for batch {BatchId}, recovered {Amount:C}",
                    unverifiedCalls.Count,
                    batchId,
                    totalRecovered);

                return RecoveryResult.CreateSuccess(
                    unverifiedCalls.Count,
                    totalRecovered,
                    $"Processed {unverifiedCalls.Count} unverified calls from {callsByStaff.Count()} staff members");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired verifications for batch {BatchId}", batchId);
                return RecoveryResult.CreateFailure($"Error processing expired verifications: {ex.Message}");
            }
        }

        /// <summary>
        /// Process verified personal calls and official calls not submitted to supervisor.
        /// Rule 1A: Verified as Personal → All calls = PERSONAL (immediate recovery)
        /// Rule 1B: Verified as Official but NOT submitted to supervisor → All calls = PERSONAL
        /// </summary>
        public async Task<RecoveryResult> ProcessVerifiedButNotSubmittedAsync(Guid batchId)
        {
            try
            {
                _logger.LogInformation("Processing verified but not submitted calls for batch {BatchId}", batchId);

                var batch = await _context.StagingBatches
                    .FirstOrDefaultAsync(b => b.Id == batchId);

                if (batch == null)
                {
                    return RecoveryResult.CreateFailure("Batch not found");
                }

                var now = DateTime.UtcNow;

                // Get all verifications that have been submitted to supervisor for this batch
                var submittedVerificationIds = await _context.CallLogVerifications
                    .Where(v => v.SubmittedToSupervisor && v.BatchId == batchId)
                    .Select(v => v.CallRecordId)
                    .ToListAsync();

                // Find calls that meet either condition:
                // 1. Verified as Personal (regardless of submission) - after verification deadline
                // 2. Verified as Official BUT not submitted to supervisor - after verification deadline
                // Note: We check ALL calls in the batch, regardless of batch status
                var callsToRecover = await _context.CallRecords
                    .Where(cr => cr.SourceBatchId == batchId
                              && cr.VerificationPeriod.HasValue
                              && cr.VerificationPeriod.Value < now  // Verification deadline has passed
                              && cr.IsVerified == true  // Staff DID verify
                              && !submittedVerificationIds.Contains(cr.Id)  // NOT submitted to supervisor
                              && (cr.AssignmentStatus == "None" || cr.AssignmentStatus == null || cr.AssignmentStatus == "")
                              && (cr.RecoveryStatus == "NotProcessed" || cr.RecoveryStatus == null || cr.RecoveryStatus == ""))
                    .ToListAsync();

                if (callsToRecover.Count == 0)
                {
                    _logger.LogInformation("No verified but not submitted calls found for batch {BatchId}", batchId);
                    return RecoveryResult.CreateSuccess(0, 0, "No verified but not submitted calls to process");
                }

                var recoveryLogs = new List<RecoveryLog>();
                decimal totalRecovered = 0;
                int personalCallsRecovered = 0;
                int officialNotSubmittedRecovered = 0;

                // Group by staff member for tracking
                var callsByStaff = callsToRecover.GroupBy(c => c.ResponsibleIndexNumber ?? "Unknown");

                foreach (var staffGroup in callsByStaff)
                {
                    var staffIndex = staffGroup.Key;
                    var staffCalls = staffGroup.ToList();

                    foreach (var call in staffCalls)
                    {
                        // Skip auto-official calls — always treated as Official
                        if (call.CallType != null && call.CallType.StartsWith(AutoOfficialCallTypePrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            call.AssignmentStatus = "Official";
                            call.FinalAssignmentType = "Official";
                            call.RecoveryStatus = "Processed";
                            call.RecoveryDate = DateTime.UtcNow;
                            call.RecoveryProcessedBy = "System-AutoRecovery";
                            call.RecoveryAmount = 0;

                            recoveryLogs.Add(new RecoveryLog
                            {
                                CallRecordId = call.Id,
                                BatchId = call.SourceBatchId ?? Guid.Empty,
                                RecoveryType = "AutoOfficialExemption",
                                RecoveryAction = "Official",
                                RecoveryDate = DateTime.UtcNow,
                                RecoveryReason = $"Call type '{call.CallType}' is auto-official (Corporate Value Pack Data) and exempt from personal recovery.",
                                AmountRecovered = 0,
                                RecoveredFrom = staffIndex,
                                ProcessedBy = "System-AutoRecovery",
                                DeadlineDate = call.VerificationPeriod!.Value,
                                IsAutomated = true
                            });
                            continue;
                        }

                        // Determine recovery reason based on verification type
                        string recoveryReason;
                        string recoveryType;

                        if (call.VerificationType == "Personal")
                        {
                            recoveryReason = $"Call verified as Personal by staff. Verification deadline: {call.VerificationPeriod!.Value:yyyy-MM-dd HH:mm}. Automatically recovered as personal.";
                            recoveryType = "VerifiedAsPersonal";
                            personalCallsRecovered++;
                        }
                        else // Official but not submitted
                        {
                            recoveryReason = $"Call verified as Official but NOT submitted to supervisor by deadline: {call.VerificationPeriod!.Value:yyyy-MM-dd HH:mm}. Automatically recovered as personal.";
                            recoveryType = "OfficialNotSubmitted";
                            officialNotSubmittedRecovered++;
                        }

                        call.AssignmentStatus = "Personal";
                        call.FinalAssignmentType = "Personal";
                        call.RecoveryStatus = "Processed";
                        call.RecoveryDate = DateTime.UtcNow;
                        call.RecoveryProcessedBy = "System-AutoRecovery";
                        call.RecoveryAmount = call.CallCost;

                        totalRecovered += call.RecoveryAmount ?? 0;

                        recoveryLogs.Add(new RecoveryLog
                        {
                            CallRecordId = call.Id,
                            BatchId = call.SourceBatchId ?? Guid.Empty,
                            RecoveryType = recoveryType,
                            RecoveryAction = "Personal",
                            RecoveryDate = DateTime.UtcNow,
                            RecoveryReason = recoveryReason,
                            AmountRecovered = call.RecoveryAmount ?? 0,
                            RecoveredFrom = staffIndex,
                            ProcessedBy = "System-AutoRecovery",
                            DeadlineDate = call.VerificationPeriod!.Value,
                            IsAutomated = true
                        });
                    }

                    _logger.LogInformation(
                        "Processed {Count} verified but not submitted calls for staff {IndexNumber}, total amount: {Amount:C}",
                        staffCalls.Count,
                        staffIndex,
                        staffCalls.Sum(c => c.RecoveryAmount ?? 0));
                }

                // Save recovery logs
                if (recoveryLogs.Any())
                {
                    _context.RecoveryLogs.AddRange(recoveryLogs);

                    // Update batch totals
                    batch.TotalPersonalAmount = (batch.TotalPersonalAmount ?? 0) + totalRecovered;
                    batch.TotalRecoveredAmount = (batch.TotalRecoveredAmount ?? 0) + totalRecovered;

                    await _context.SaveChangesAsync();

                    // Send notifications to affected staff
                    var earliestDeadline = callsToRecover
                        .Where(c => c.VerificationPeriod.HasValue)
                        .Min(c => c.VerificationPeriod!.Value);
                    await NotifyStaffOfRecoveryAsync(callsByStaff.Select(g => g.Key).ToList(), "VerifiedButNotSubmitted", earliestDeadline);
                }

                _logger.LogInformation(
                    "Successfully processed {Total} verified but not submitted calls for batch {BatchId}: {Personal} Personal, {Official} Official not submitted, recovered {Amount:C}",
                    callsToRecover.Count,
                    batchId,
                    personalCallsRecovered,
                    officialNotSubmittedRecovered,
                    totalRecovered);

                return RecoveryResult.CreateSuccess(
                    callsToRecover.Count,
                    totalRecovered,
                    $"Processed {callsToRecover.Count} calls ({personalCallsRecovered} Personal, {officialNotSubmittedRecovered} Official not submitted) from {callsByStaff.Count()} staff members");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing verified but not submitted calls for batch {BatchId}", batchId);
                return RecoveryResult.CreateFailure($"Error processing verified but not submitted calls: {ex.Message}");
            }
        }

        /// <summary>
        /// Process expired supervisor approval deadlines.
        /// Rule 2: Supervisor non-approval → All calls = CLASS OF SERVICE
        /// This checks CallRecord.ApprovalPeriod directly for verified but unapproved records
        /// </summary>
        public async Task<RecoveryResult> ProcessExpiredApprovalsAsync(Guid batchId)
        {
            try
            {
                _logger.LogInformation("Processing expired approvals for batch {BatchId}", batchId);

                var batch = await _context.StagingBatches
                    .FirstOrDefaultAsync(b => b.Id == batchId);

                if (batch == null)
                {
                    return RecoveryResult.CreateFailure("Batch not found");
                }

                var now = DateTime.UtcNow;

                // Get all CallRecords where:
                // 1. Approval deadline has passed
                // 2. Record is verified (staff DID verify)
                // 3. Supervisor has NOT approved
                // 4. Not already recovered
                var expiredApprovalRecords = await _context.CallRecords
                    .Where(cr => cr.SourceBatchId == batchId
                              && cr.ApprovalPeriod.HasValue
                              && cr.ApprovalPeriod.Value < now  // Approval deadline has passed
                              && cr.IsVerified == true  // Staff DID verify
                              && (cr.AssignmentStatus == "None" || cr.AssignmentStatus == null)
                              && (cr.RecoveryStatus == "NotProcessed" || cr.RecoveryStatus == null))
                    .ToListAsync();

                if (expiredApprovalRecords.Count == 0)
                {
                    _logger.LogInformation("No expired approvals found for batch {BatchId}", batchId);
                    return RecoveryResult.CreateSuccess(0, 0, "No pending approvals to process");
                }

                // Get their verification records to check supervisor status and get supervisor info
                var callRecordIds = expiredApprovalRecords.Select(cr => cr.Id).ToList();
                var verifications = await _context.CallLogVerifications
                    .Where(v => callRecordIds.Contains(v.CallRecordId))
                    .ToListAsync();

                // Create a dictionary for quick lookup
                var verificationMap = verifications.ToDictionary(v => v.CallRecordId);

                var recoveryLogs = new List<RecoveryLog>();
                decimal totalRecovered = 0;
                int callsProcessed = 0;

                foreach (var call in expiredApprovalRecords)
                {
                    // Check if supervisor approved - if approved, skip recovery
                    var verification = verificationMap.GetValueOrDefault(call.Id);
                    if (verification?.SupervisorApprovalStatus == "Approved")
                    {
                        _logger.LogInformation("Skipping CallRecord {CallRecordId} - already approved by supervisor", call.Id);
                        continue;
                    }

                    call.AssignmentStatus = "ClassOfService";
                    call.FinalAssignmentType = "ClassOfService";
                    call.RecoveryStatus = "Processed";
                    call.RecoveryDate = DateTime.UtcNow;
                    call.RecoveryProcessedBy = "System-AutoRecovery";

                    // Calculate class of service amount
                    var classOfServiceAmount = await CalculateClassOfServiceAmountAsync(call.Id);
                    call.RecoveryAmount = classOfServiceAmount;
                    totalRecovered += classOfServiceAmount;
                    callsProcessed++;

                    var supervisorIndexNumber = verification?.SupervisorIndexNumber ?? "Unknown";
                    var approvalDeadline = call.ApprovalPeriod!.Value;

                    recoveryLogs.Add(new RecoveryLog
                    {
                        CallRecordId = call.Id,
                        BatchId = call.SourceBatchId ?? Guid.Empty,
                        RecoveryType = "SupervisorNonApproval",
                        RecoveryAction = "ClassOfService",
                        RecoveryDate = DateTime.UtcNow,
                        RecoveryReason = $"Supervisor '{supervisorIndexNumber}' failed to approve by deadline: {approvalDeadline:yyyy-MM-dd HH:mm}. Call recovered as per class of service.",
                        AmountRecovered = classOfServiceAmount,
                        RecoveredFrom = supervisorIndexNumber,
                        ProcessedBy = "System-AutoRecovery",
                        DeadlineDate = approvalDeadline,
                        IsAutomated = true
                    });

                    // Mark verification as deadline missed (if exists)
                    if (verification != null)
                    {
                        verification.DeadlineMissed = true;
                        verification.SupervisorApprovalStatus = "DeadlineMissed";
                    }

                    _logger.LogInformation(
                        "Recovered CallRecord {CallRecordId} as CLASS OF SERVICE. Approval deadline was {ApprovalDeadline}, supervisor: {Supervisor}",
                        call.Id, approvalDeadline, supervisorIndexNumber);
                }

                // Save recovery logs
                if (recoveryLogs.Any())
                {
                    _context.RecoveryLogs.AddRange(recoveryLogs);

                    // Update batch totals
                    batch.TotalClassOfServiceAmount = (batch.TotalClassOfServiceAmount ?? 0) + totalRecovered;
                    batch.TotalRecoveredAmount = (batch.TotalRecoveredAmount ?? 0) + totalRecovered;

                    await _context.SaveChangesAsync();

                    // Notify supervisors
                    var supervisors = verifications
                        .Select(v => v.SupervisorIndexNumber)
                        .Where(s => !string.IsNullOrEmpty(s))
                        .Distinct()
                        .ToList();

                    if (supervisors.Any())
                    {
                        // Use the earliest approval deadline for notification
                        var earliestDeadline = expiredApprovalRecords
                            .Where(cr => cr.ApprovalPeriod.HasValue)
                            .Min(cr => cr.ApprovalPeriod!.Value);
                        await NotifySupervisorsOfMissedDeadlineAsync(supervisors!, earliestDeadline);
                    }
                }

                _logger.LogInformation(
                    "Successfully processed {Count} expired approvals for batch {BatchId}, recovered {Amount:C}",
                    callsProcessed,
                    batchId,
                    totalRecovered);

                return RecoveryResult.CreateSuccess(
                    callsProcessed,
                    totalRecovered,
                    $"Processed {callsProcessed} calls with missed approval deadlines");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired approvals for batch {BatchId}", batchId);
                return RecoveryResult.CreateFailure($"Error processing expired approvals: {ex.Message}");
            }
        }

        /// <summary>
        /// Process supervisor partial approval.
        /// Rule 3: Supervisor partial approval → Approved = OFFICIAL, Rest = PERSONAL
        /// </summary>
        public async Task<RecoveryResult> ProcessPartialApprovalAsync(int verificationId, List<int> approvedCallIds, string supervisorIndexNumber)
        {
            try
            {
                _logger.LogInformation(
                    "Processing partial approval for verification {VerificationId} by supervisor {Supervisor}",
                    verificationId,
                    supervisorIndexNumber);

                var verification = await _context.CallLogVerifications
                    .Include(v => v.CallRecord)
                    .FirstOrDefaultAsync(v => v.Id == verificationId);

                if (verification == null)
                {
                    return RecoveryResult.CreateFailure("Verification not found");
                }

                // Get all calls associated with this verification (might need to handle multiple calls per verification)
                var calls = await _context.CallRecords
                    .Where(cr => cr.Id == verification.CallRecordId)
                    .ToListAsync();

                var recoveryLogs = new List<RecoveryLog>();
                decimal totalRecovered = 0;
                int approvedCount = 0;
                int notApprovedCount = 0;

                foreach (var call in calls)
                {
                    bool isApproved = approvedCallIds.Contains(call.Id);

                    if (isApproved)
                    {
                        // Approved calls
                        call.AssignmentStatus = verification.VerificationType.ToString();
                        call.FinalAssignmentType = verification.VerificationType == VerificationType.Personal ? "Personal" : "Official";
                        call.SupervisorApprovalStatus = "Approved";
                        approvedCount++;
                    }
                    else
                    {
                        // Non-approved calls become Personal, unless auto-official
                        if (call.CallType != null && call.CallType.StartsWith(AutoOfficialCallTypePrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            call.AssignmentStatus = "Official";
                            call.FinalAssignmentType = "Official";
                            call.SupervisorApprovalStatus = "Approved";
                            approvedCount++;
                        }
                        else
                        {
                            call.AssignmentStatus = "Personal";
                            call.FinalAssignmentType = "Personal";
                            call.SupervisorApprovalStatus = "PartiallyApproved";
                            notApprovedCount++;
                        }
                    }

                    call.RecoveryStatus = "Processed";
                    call.RecoveryDate = DateTime.UtcNow;
                    call.RecoveryProcessedBy = supervisorIndexNumber;
                    call.RecoveryAmount = call.CallCost; // Use original currency amount
                    call.SupervisorApprovedBy = supervisorIndexNumber;
                    call.SupervisorApprovedDate = DateTime.UtcNow;

                    totalRecovered += call.RecoveryAmount ?? 0;

                    recoveryLogs.Add(new RecoveryLog
                    {
                        CallRecordId = call.Id,
                        BatchId = call.SourceBatchId ?? Guid.Empty,
                        RecoveryType = "SupervisorPartialApproval",
                        RecoveryAction = call.FinalAssignmentType!,
                        RecoveryDate = DateTime.UtcNow,
                        RecoveryReason = isApproved
                            ? $"Approved by supervisor {supervisorIndexNumber}"
                            : $"Not approved by supervisor {supervisorIndexNumber} - recovered as personal",
                        AmountRecovered = call.RecoveryAmount ?? 0,
                        RecoveredFrom = call.ResponsibleIndexNumber,
                        ProcessedBy = supervisorIndexNumber,
                        IsAutomated = false
                    });
                }

                // Update verification status
                verification.SupervisorApprovalStatus = "PartiallyApproved";
                verification.SupervisorApprovedBy = supervisorIndexNumber;
                verification.SupervisorApprovedDate = DateTime.UtcNow;

                // Save recovery logs
                _context.RecoveryLogs.AddRange(recoveryLogs);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Partial approval processed: {Approved} approved, {NotApproved} not approved, total {Amount:C}",
                    approvedCount,
                    notApprovedCount,
                    totalRecovered);

                return RecoveryResult.CreateSuccess(
                    calls.Count,
                    totalRecovered,
                    $"Partial approval completed: {approvedCount} approved, {notApprovedCount} marked as personal");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing partial approval for verification {VerificationId}", verificationId);
                return RecoveryResult.CreateFailure($"Error processing partial approval: {ex.Message}");
            }
        }

        /// <summary>
        /// Process reverted verifications that missed re-verification deadline.
        /// Rule 4: Supervisor revert + staff failure → All calls = PERSONAL
        /// </summary>
        public async Task<RecoveryResult> ProcessRevertedVerificationsAsync(Guid batchId)
        {
            try
            {
                _logger.LogInformation("Processing reverted verifications for batch {BatchId}", batchId);

                // Find verifications that were reverted and deadline passed
                var revertedExpired = await _context.CallLogVerifications
                    .Include(v => v.CallRecord)
                    .Where(v => v.BatchId == batchId
                             && v.SupervisorApprovalStatus == "Reverted"
                             && v.RevertDeadline != null
                             && v.RevertDeadline < DateTime.UtcNow
                             && !v.SubmittedToSupervisor // Not re-submitted
                             && (v.CallRecord.RecoveryStatus == "NotProcessed" || v.CallRecord.RecoveryStatus == null))
                    .ToListAsync();

                if (revertedExpired.Count == 0)
                {
                    _logger.LogInformation("No reverted expired verifications found for batch {BatchId}", batchId);
                    return RecoveryResult.CreateSuccess(0, 0, "No reverted expired verifications to process");
                }

                var recoveryLogs = new List<RecoveryLog>();
                decimal totalRecovered = 0;
                int callsProcessed = 0;

                foreach (var verification in revertedExpired)
                {
                    var call = verification.CallRecord;

                    // Skip auto-official calls — always treated as Official
                    if (call.CallType != null && call.CallType.StartsWith(AutoOfficialCallTypePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        call.AssignmentStatus = "Official";
                        call.FinalAssignmentType = "Official";
                        call.RecoveryStatus = "Processed";
                        call.RecoveryDate = DateTime.UtcNow;
                        call.RecoveryProcessedBy = "System-AutoRecovery";
                        call.RecoveryAmount = 0;

                        recoveryLogs.Add(new RecoveryLog
                        {
                            CallRecordId = call.Id,
                            BatchId = call.SourceBatchId ?? Guid.Empty,
                            RecoveryType = "AutoOfficialExemption",
                            RecoveryAction = "Official",
                            RecoveryDate = DateTime.UtcNow,
                            RecoveryReason = $"Call type '{call.CallType}' is auto-official (Corporate Value Pack Data) and exempt from personal recovery.",
                            AmountRecovered = 0,
                            RecoveredFrom = call.ResponsibleIndexNumber,
                            ProcessedBy = "System-AutoRecovery",
                            DeadlineDate = verification.RevertDeadline,
                            IsAutomated = true
                        });
                        verification.DeadlineMissed = true;
                        callsProcessed++;
                        continue;
                    }

                    call.AssignmentStatus = "Personal";
                    call.FinalAssignmentType = "Personal";
                    call.RecoveryStatus = "Processed";
                    call.RecoveryDate = DateTime.UtcNow;
                    call.RecoveryProcessedBy = "System-AutoRecovery";
                    call.RecoveryAmount = call.CallCost; // Use original currency amount

                    totalRecovered += call.RecoveryAmount ?? 0;
                    callsProcessed++;

                    recoveryLogs.Add(new RecoveryLog
                    {
                        CallRecordId = call.Id,
                        BatchId = call.SourceBatchId ?? Guid.Empty,
                        RecoveryType = "SupervisorRevertFailure",
                        RecoveryAction = "Personal",
                        RecoveryDate = DateTime.UtcNow,
                        RecoveryReason = $"Staff failed to re-verify after supervisor revert. Revert deadline: {verification.RevertDeadline:yyyy-MM-dd HH:mm}. Revert count: {verification.RevertCount}",
                        AmountRecovered = call.RecoveryAmount ?? 0,
                        RecoveredFrom = call.ResponsibleIndexNumber,
                        ProcessedBy = "System-AutoRecovery",
                        DeadlineDate = verification.RevertDeadline,
                        IsAutomated = true
                    });

                    verification.DeadlineMissed = true;
                }

                // Save recovery logs
                if (recoveryLogs.Any())
                {
                    _context.RecoveryLogs.AddRange(recoveryLogs);

                    // Update batch totals
                    var batch = await _context.StagingBatches.FindAsync(batchId);
                    if (batch != null)
                    {
                        batch.TotalPersonalAmount = (batch.TotalPersonalAmount ?? 0) + totalRecovered;
                        batch.TotalRecoveredAmount = (batch.TotalRecoveredAmount ?? 0) + totalRecovered;
                    }

                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation(
                    "Processed {Count} reverted expired verifications for batch {BatchId}, recovered {Amount:C}",
                    callsProcessed,
                    batchId,
                    totalRecovered);

                return RecoveryResult.CreateSuccess(
                    callsProcessed,
                    totalRecovered,
                    $"Processed {callsProcessed} reverted verifications with missed deadlines");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing reverted verifications for batch {BatchId}", batchId);
                return RecoveryResult.CreateFailure($"Error processing reverted verifications: {ex.Message}");
            }
        }

        /// <summary>
        /// Process full supervisor approval.
        /// Rule 5: Supervisor full approval → All calls = OFFICIAL (as verified by staff)
        /// </summary>
        public async Task<RecoveryResult> ProcessFullApprovalAsync(int verificationId, string supervisorIndexNumber)
        {
            try
            {
                _logger.LogInformation(
                    "Processing full approval for verification {VerificationId} by supervisor {Supervisor}",
                    verificationId,
                    supervisorIndexNumber);

                var verification = await _context.CallLogVerifications
                    .Include(v => v.CallRecord)
                    .FirstOrDefaultAsync(v => v.Id == verificationId);

                if (verification == null)
                {
                    return RecoveryResult.CreateFailure("Verification not found");
                }

                var call = verification.CallRecord;

                // Approve as verified by staff
                call.AssignmentStatus = verification.VerificationType.ToString();
                call.FinalAssignmentType = verification.VerificationType == VerificationType.Personal ? "Personal" : "Official";
                call.RecoveryStatus = "Processed";
                call.RecoveryDate = DateTime.UtcNow;
                call.RecoveryProcessedBy = supervisorIndexNumber;
                call.RecoveryAmount = call.CallCost; // Use original currency amount
                call.SupervisorApprovalStatus = "Approved";
                call.SupervisorApprovedBy = supervisorIndexNumber;
                call.SupervisorApprovedDate = DateTime.UtcNow;

                // Update verification
                verification.SupervisorApprovalStatus = "Approved";
                verification.SupervisorApprovedBy = supervisorIndexNumber;
                verification.SupervisorApprovedDate = DateTime.UtcNow;
                verification.ApprovedAmount = call.CallCost; // Use original currency amount

                // Create recovery log
                var recoveryLog = new RecoveryLog
                {
                    CallRecordId = call.Id,
                    BatchId = call.SourceBatchId ?? Guid.Empty,
                    RecoveryType = "SupervisorPartialApproval", // Using same type for full approval
                    RecoveryAction = call.FinalAssignmentType!,
                    RecoveryDate = DateTime.UtcNow,
                    RecoveryReason = $"Fully approved by supervisor {supervisorIndexNumber}",
                    AmountRecovered = call.RecoveryAmount ?? 0,
                    RecoveredFrom = call.ResponsibleIndexNumber,
                    ProcessedBy = supervisorIndexNumber,
                    IsAutomated = false
                };

                _context.RecoveryLogs.Add(recoveryLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Full approval processed for verification {VerificationId}, amount {Amount:C}",
                    verificationId,
                    call.RecoveryAmount ?? 0);

                return RecoveryResult.CreateSuccess(
                    1,
                    call.RecoveryAmount ?? 0,
                    "Full approval completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing full approval for verification {VerificationId}", verificationId);
                return RecoveryResult.CreateFailure($"Error processing full approval: {ex.Message}");
            }
        }

        /// <summary>
        /// Process supervisor rejection.
        /// Rule 6: Supervisor rejection → All calls = PERSONAL
        /// </summary>
        public async Task<RecoveryResult> ProcessRejectionAsync(int verificationId, string supervisorIndexNumber, string rejectionReason)
        {
            try
            {
                _logger.LogInformation(
                    "Processing rejection for verification {VerificationId} by supervisor {Supervisor}",
                    verificationId,
                    supervisorIndexNumber);

                var verification = await _context.CallLogVerifications
                    .Include(v => v.CallRecord)
                    .FirstOrDefaultAsync(v => v.Id == verificationId);

                if (verification == null)
                {
                    return RecoveryResult.CreateFailure("Verification not found");
                }

                var call = verification.CallRecord;

                // Auto-official calls cannot be rejected to Personal
                if (call.CallType != null && call.CallType.StartsWith(AutoOfficialCallTypePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    call.AssignmentStatus = "Official";
                    call.FinalAssignmentType = "Official";
                    call.RecoveryStatus = "Processed";
                    call.RecoveryDate = DateTime.UtcNow;
                    call.RecoveryProcessedBy = supervisorIndexNumber;
                    call.RecoveryAmount = 0;
                    call.SupervisorApprovalStatus = "Approved";
                    call.SupervisorApprovedBy = supervisorIndexNumber;
                    call.SupervisorApprovedDate = DateTime.UtcNow;

                    verification.SupervisorApprovalStatus = "Approved";
                    verification.SupervisorApprovedBy = supervisorIndexNumber;
                    verification.SupervisorApprovedDate = DateTime.UtcNow;

                    _context.RecoveryLogs.Add(new RecoveryLog
                    {
                        CallRecordId = call.Id,
                        BatchId = call.SourceBatchId ?? Guid.Empty,
                        RecoveryType = "AutoOfficialExemption",
                        RecoveryAction = "Official",
                        RecoveryDate = DateTime.UtcNow,
                        RecoveryReason = $"Call type '{call.CallType}' is auto-official (Corporate Value Pack Data). Rejection overridden to Official.",
                        AmountRecovered = 0,
                        RecoveredFrom = call.ResponsibleIndexNumber,
                        ProcessedBy = supervisorIndexNumber,
                        IsAutomated = false
                    });
                    await _context.SaveChangesAsync();

                    return RecoveryResult.CreateSuccess(1, 0, "Auto-official call cannot be rejected. Marked as Official.");
                }

                // Reject - mark as personal
                call.AssignmentStatus = "Personal";
                call.FinalAssignmentType = "Personal";
                call.RecoveryStatus = "Processed";
                call.RecoveryDate = DateTime.UtcNow;
                call.RecoveryProcessedBy = supervisorIndexNumber;
                call.RecoveryAmount = call.CallCost; // Use original currency amount
                call.SupervisorApprovalStatus = "Rejected";
                call.SupervisorApprovedBy = supervisorIndexNumber;
                call.SupervisorApprovedDate = DateTime.UtcNow;

                // Update verification
                verification.SupervisorApprovalStatus = "Rejected";
                verification.SupervisorApprovedBy = supervisorIndexNumber;
                verification.SupervisorApprovedDate = DateTime.UtcNow;
                verification.RejectionReason = rejectionReason;

                // Create recovery log
                var recoveryLog = new RecoveryLog
                {
                    CallRecordId = call.Id,
                    BatchId = call.SourceBatchId ?? Guid.Empty,
                    RecoveryType = "SupervisorRejection",
                    RecoveryAction = "Personal",
                    RecoveryDate = DateTime.UtcNow,
                    RecoveryReason = $"Rejected by supervisor {supervisorIndexNumber}. Reason: {rejectionReason}",
                    AmountRecovered = call.RecoveryAmount ?? 0,
                    RecoveredFrom = call.ResponsibleIndexNumber,
                    ProcessedBy = supervisorIndexNumber,
                    IsAutomated = false
                };

                _context.RecoveryLogs.Add(recoveryLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Rejection processed for verification {VerificationId}, amount {Amount:C}",
                    verificationId,
                    call.RecoveryAmount ?? 0);

                return RecoveryResult.CreateSuccess(
                    1,
                    call.RecoveryAmount ?? 0,
                    "Rejection completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing rejection for verification {VerificationId}", verificationId);
                return RecoveryResult.CreateFailure($"Error processing rejection: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculate recovery for a specific staff member and period
        /// </summary>
        public async Task<StaffRecoveryDetail> CalculateRecoveryForStaffAsync(string indexNumber, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.RecoveryLogs
                .Include(rl => rl.CallRecord)
                .Where(rl => rl.RecoveredFrom == indexNumber);

            if (startDate.HasValue)
                query = query.Where(rl => rl.RecoveryDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(rl => rl.RecoveryDate <= endDate.Value);

            var logs = await query.ToListAsync();

            var detail = new StaffRecoveryDetail
            {
                IndexNumber = indexNumber,
                TotalCalls = logs.Count,
                TotalAmount = logs.Sum(l => l.AmountRecovered),
                PersonalCalls = logs.Count(l => l.RecoveryAction == "Personal"),
                PersonalAmount = logs.Where(l => l.RecoveryAction == "Personal").Sum(l => l.AmountRecovered),
                OfficialCalls = logs.Count(l => l.RecoveryAction == "Official"),
                OfficialAmount = logs.Where(l => l.RecoveryAction == "Official").Sum(l => l.AmountRecovered),
                ClassOfServiceCalls = logs.Count(l => l.RecoveryAction == "ClassOfService"),
                ClassOfServiceAmount = logs.Where(l => l.RecoveryAction == "ClassOfService").Sum(l => l.AmountRecovered),
                MissedDeadlines = logs.Count(l => l.RecoveryType == "StaffNonVerification" || l.RecoveryType == "SupervisorRevertFailure")
            };

            // Get user details
            var user = await _context.EbillUsers.FirstOrDefaultAsync(u => u.IndexNumber == indexNumber);
            if (user != null)
            {
                detail.FirstName = user.FirstName;
                detail.LastName = user.LastName;
                detail.Email = user.Email;
            }

            return detail;
        }

        /// <summary>
        /// Calculate class of service amount for a call record
        /// </summary>
        public async Task<decimal> CalculateClassOfServiceAmountAsync(int callRecordId)
        {
            var call = await _context.CallRecords
                .Include(c => c.UserPhone)
                .ThenInclude(up => up!.ClassOfService)
                .FirstOrDefaultAsync(c => c.Id == callRecordId);

            if (call?.UserPhone?.ClassOfService != null)
            {
                // Return the handset allowance amount or pro-rated amount based on usage
                return call.UserPhone.ClassOfService.HandsetAllowanceAmount ?? call.CallCost; // Use original currency amount
            }

            // Default to actual call cost if no class of service found
            return call?.CallCost ?? 0; // Use original currency amount
        }

        /// <summary>
        /// Manual recovery override for exceptional cases
        /// </summary>
        public async Task<RecoveryResult> ManualRecoveryOverrideAsync(int callRecordId, string recoveryAction, string reason, string performedBy)
        {
            try
            {
                var call = await _context.CallRecords.FindAsync(callRecordId);
                if (call == null)
                {
                    return RecoveryResult.CreateFailure("Call record not found");
                }

                // Update call record
                call.AssignmentStatus = recoveryAction;
                call.FinalAssignmentType = recoveryAction;
                call.RecoveryStatus = "ManuallyOverridden";
                call.RecoveryDate = DateTime.UtcNow;
                call.RecoveryProcessedBy = performedBy;
                call.RecoveryAmount = call.CallCost; // Use original currency amount

                // Create recovery log
                var recoveryLog = new RecoveryLog
                {
                    CallRecordId = call.Id,
                    BatchId = call.SourceBatchId ?? Guid.Empty,
                    RecoveryType = "ManualOverride",
                    RecoveryAction = recoveryAction,
                    RecoveryDate = DateTime.UtcNow,
                    RecoveryReason = $"Manual override by {performedBy}. Reason: {reason}",
                    AmountRecovered = call.RecoveryAmount ?? 0,
                    RecoveredFrom = call.ResponsibleIndexNumber,
                    ProcessedBy = performedBy,
                    IsAutomated = false
                };

                _context.RecoveryLogs.Add(recoveryLog);
                await _context.SaveChangesAsync();

                return RecoveryResult.CreateSuccess(1, call.RecoveryAmount ?? 0, "Manual override completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing manual recovery override for call {CallId}", callRecordId);
                return RecoveryResult.CreateFailure($"Error performing manual override: {ex.Message}");
            }
        }

        /// <summary>
        /// Get recovery statistics for a batch
        /// </summary>
        public async Task<BatchAnalysisReport> GetBatchRecoveryStatisticsAsync(Guid batchId)
        {
            var batch = await _context.StagingBatches.FindAsync(batchId);
            if (batch == null)
            {
                throw new ArgumentException("Batch not found", nameof(batchId));
            }

            var recoveryLogs = await _context.RecoveryLogs
                .Where(rl => rl.BatchId == batchId)
                .ToListAsync();

            var report = new BatchAnalysisReport
            {
                BatchId = batchId,
                BatchName = batch.BatchName,
                CreatedDate = batch.CreatedDate,
                TotalAmount = batch.TotalRecoveredAmount ?? 0,
                PersonalRecoveryCalls = recoveryLogs.Count(l => l.RecoveryAction == "Personal"),
                PersonalRecoveryAmount = recoveryLogs.Where(l => l.RecoveryAction == "Personal").Sum(l => l.AmountRecovered),
                OfficialRecoveryCalls = recoveryLogs.Count(l => l.RecoveryAction == "Official"),
                OfficialRecoveryAmount = recoveryLogs.Where(l => l.RecoveryAction == "Official").Sum(l => l.AmountRecovered),
                ClassOfServiceCalls = recoveryLogs.Count(l => l.RecoveryAction == "ClassOfService"),
                ClassOfServiceAmount = recoveryLogs.Where(l => l.RecoveryAction == "ClassOfService").Sum(l => l.AmountRecovered),
                TotalCalls = recoveryLogs.Count
            };

            return report;
        }

        #region Private Helper Methods

        private async Task NotifyStaffOfRecoveryAsync(List<string> indexNumbers, string recoveryType, DateTime deadline)
        {
            try
            {
                foreach (var indexNumber in indexNumbers)
                {
                    // Find EbillUser by IndexNumber
                    var ebillUser = await _context.EbillUsers.FirstOrDefaultAsync(u => u.IndexNumber == indexNumber);
                    if (ebillUser != null && !string.IsNullOrEmpty(ebillUser.Email))
                    {
                        // Find corresponding AspNetUser by email
                        var aspNetUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == ebillUser.Email);
                        if (aspNetUser != null)
                        {
                            await _notificationService.CreateNotificationAsync(
                                aspNetUser.Id,
                                "Call Log Recovery Processed",
                                $"Your calls have been automatically recovered as PERSONAL due to missed verification deadline ({deadline:yyyy-MM-dd HH:mm}).",
                                NotificationType.Warning
                            );
                        }
                        else
                        {
                            _logger.LogWarning("No AspNetUser found for EbillUser {IndexNumber} with email {Email}", indexNumber, ebillUser.Email);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending recovery notifications to staff");
            }
        }

        private async Task NotifySupervisorsOfMissedDeadlineAsync(List<string> supervisorIndexNumbers, DateTime deadline)
        {
            try
            {
                foreach (var indexNumber in supervisorIndexNumbers)
                {
                    // Find EbillUser by IndexNumber
                    var ebillUser = await _context.EbillUsers.FirstOrDefaultAsync(u => u.IndexNumber == indexNumber);
                    if (ebillUser != null && !string.IsNullOrEmpty(ebillUser.Email))
                    {
                        // Find corresponding AspNetUser by email
                        var aspNetUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == ebillUser.Email);
                        if (aspNetUser != null)
                        {
                            await _notificationService.CreateNotificationAsync(
                                aspNetUser.Id,
                                "Approval Deadline Missed",
                                $"Call logs have been automatically recovered as CLASS OF SERVICE due to missed approval deadline ({deadline:yyyy-MM-dd HH:mm}).",
                                NotificationType.Warning
                            );
                        }
                        else
                        {
                            _logger.LogWarning("No AspNetUser found for supervisor {IndexNumber} with email {Email}", indexNumber, ebillUser.Email);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending missed deadline notifications to supervisors");
            }
        }

        #endregion
    }
}
