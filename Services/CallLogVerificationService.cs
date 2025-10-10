using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Models.Enums;

namespace TAB.Web.Services
{
    public class CallLogVerificationService : ICallLogVerificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CallLogVerificationService> _logger;
        private readonly IAuditLogService _auditLogService;
        private readonly INotificationService _notificationService;

        public CallLogVerificationService(
            ApplicationDbContext context,
            ILogger<CallLogVerificationService> logger,
            IAuditLogService auditLogService,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
        }

        // ===================================================================
        // User Verification Operations
        // ===================================================================

        public async Task<CallLogVerification> VerifyCallLogAsync(
            int callRecordId,
            string indexNumber,
            VerificationType verificationType,
            string? justification = null,
            List<IFormFile>? documents = null)
        {
            try
            {
                var callRecord = await _context.CallRecords
                    .Include(c => c.ResponsibleUser)
                    .FirstOrDefaultAsync(c => c.Id == callRecordId);

                if (callRecord == null)
                    throw new ArgumentException($"Call record with ID {callRecordId} not found");

                // Verify user has permission to verify this call
                // User can verify if they are either the responsible user OR the paying user (assigned and accepted)
                bool isResponsibleUser = callRecord.ResponsibleIndexNumber == indexNumber;
                bool isPayingUser = callRecord.PayingIndexNumber == indexNumber &&
                                   callRecord.AssignmentStatus == "Accepted";

                if (!isResponsibleUser && !isPayingUser)
                    throw new UnauthorizedAccessException("You can only verify call records that belong to you or have been assigned to you and accepted");

                // Check if call has been assigned out and is awaiting acceptance
                if (callRecord.AssignmentStatus == "Pending" &&
                    callRecord.ResponsibleIndexNumber == indexNumber &&
                    callRecord.PayingIndexNumber != indexNumber)
                {
                    throw new InvalidOperationException("Cannot verify a call that has been assigned to another user and is awaiting acceptance. The assignment must be accepted or rejected first.");
                }

                // Check if already verified
                var existingVerification = await _context.CallLogVerifications
                    .FirstOrDefaultAsync(v => v.CallRecordId == callRecordId);

                if (existingVerification != null && existingVerification.SubmittedToSupervisor)
                    throw new InvalidOperationException("Cannot modify a verification that has been submitted to supervisor");

                // Get the verifying user's class of service to check allowance
                // Use the person who is verifying (could be assigned user or responsible user)
                var userPhone = await _context.UserPhones
                    .Include(up => up.ClassOfService)
                    .FirstOrDefaultAsync(up => up.IndexNumber == indexNumber && up.IsActive);

                decimal allowanceAmount = 0;
                int? classOfServiceId = null;
                bool isOverage = false;

                if (userPhone?.ClassOfService != null)
                {
                    classOfServiceId = userPhone.ClassOfService.Id;
                    // AirtimeAllowanceAmount represents the total monthly allowance (includes airtime AND data)
                    // NULL or 0 means Unlimited
                    var allowanceLimit = userPhone.ClassOfService.AirtimeAllowanceAmount;
                    allowanceAmount = allowanceLimit ?? 0;

                    // Check if this call puts user over allowance (only if limit is set)
                    if (allowanceLimit.HasValue && allowanceLimit.Value > 0)
                    {
                        var monthlyUsage = await GetMonthlyUsageAsync(indexNumber, callRecord.CallMonth, callRecord.CallYear);
                        isOverage = (monthlyUsage + callRecord.CallCostUSD) > allowanceLimit.Value;
                    }
                }

                // If overage and no justification, require justification
                if (isOverage && string.IsNullOrWhiteSpace(justification))
                {
                    throw new ArgumentException("Justification is required for calls that exceed your monthly allowance");
                }

                var verification = new CallLogVerification
                {
                    CallRecordId = callRecordId,
                    VerifiedBy = indexNumber,
                    VerifiedDate = DateTime.UtcNow,
                    VerificationType = verificationType,
                    ClassOfServiceId = classOfServiceId,
                    AllowanceAmount = allowanceAmount,
                    ActualAmount = callRecord.CallCostUSD,
                    IsOverage = isOverage,
                    JustificationText = justification,
                    ApprovalStatus = "Pending",
                    CreatedDate = DateTime.UtcNow
                };

                if (existingVerification != null)
                {
                    // Update existing
                    existingVerification.VerificationType = verificationType;
                    existingVerification.JustificationText = justification;
                    existingVerification.IsOverage = isOverage;
                    existingVerification.ModifiedDate = DateTime.UtcNow;
                    _context.CallLogVerifications.Update(existingVerification);
                }
                else
                {
                    // Create new
                    await _context.CallLogVerifications.AddAsync(verification);
                }

                // Update call record
                callRecord.IsVerified = true;
                callRecord.VerificationDate = DateTime.UtcNow;
                callRecord.VerificationType = verificationType.ToString();
                callRecord.OverageJustified = isOverage && !string.IsNullOrWhiteSpace(justification);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Call record {CallRecordId} verified by {IndexNumber} as {VerificationType}",
                    callRecordId, indexNumber, verificationType);

                return existingVerification ?? verification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying call log {CallRecordId} for user {IndexNumber}",
                    callRecordId, indexNumber);
                throw;
            }
        }

        public async Task<List<CallLogVerification>> GetUserVerificationsAsync(
            string indexNumber,
            bool pendingOnly = false)
        {
            var query = _context.CallLogVerifications
                .Include(v => v.CallRecord)
                .Include(v => v.ClassOfService)
                .Include(v => v.Documents)
                .Where(v => v.VerifiedBy == indexNumber);

            if (pendingOnly)
                query = query.Where(v => v.ApprovalStatus == "Pending");

            return await query
                .OrderByDescending(v => v.VerifiedDate)
                .ToListAsync();
        }

        public async Task<CallLogVerification?> GetVerificationByIdAsync(int verificationId)
        {
            return await _context.CallLogVerifications
                .Include(v => v.CallRecord)
                .Include(v => v.ClassOfService)
                .Include(v => v.Documents)
                .FirstOrDefaultAsync(v => v.Id == verificationId);
        }

        public async Task<bool> UpdateVerificationAsync(CallLogVerification verification)
        {
            try
            {
                if (verification.SubmittedToSupervisor)
                {
                    _logger.LogWarning("Cannot update verification {Id} - already submitted to supervisor", verification.Id);
                    return false;
                }

                verification.ModifiedDate = DateTime.UtcNow;
                _context.CallLogVerifications.Update(verification);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating verification {Id}", verification.Id);
                return false;
            }
        }

        public async Task<bool> DeleteVerificationAsync(int verificationId, string indexNumber)
        {
            try
            {
                var verification = await _context.CallLogVerifications
                    .FirstOrDefaultAsync(v => v.Id == verificationId && v.VerifiedBy == indexNumber);

                if (verification == null)
                    return false;

                if (verification.SubmittedToSupervisor)
                {
                    _logger.LogWarning("Cannot delete verification {Id} - already submitted to supervisor", verificationId);
                    return false;
                }

                _context.CallLogVerifications.Remove(verification);

                // Update call record
                var callRecord = await _context.CallRecords.FindAsync(verification.CallRecordId);
                if (callRecord != null)
                {
                    callRecord.IsVerified = false;
                    callRecord.VerificationDate = null;
                }

                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting verification {Id}", verificationId);
                return false;
            }
        }

        // ===================================================================
        // Overage Detection & Calculation
        // ===================================================================

        public async Task<bool> IsOverageAsync(int callRecordId, string indexNumber)
        {
            var callRecord = await _context.CallRecords.FindAsync(callRecordId);
            if (callRecord == null) return false;

            var monthlyUsage = await GetMonthlyUsageAsync(indexNumber, callRecord.CallMonth, callRecord.CallYear);

            var userPhone = await _context.UserPhones
                .Include(up => up.ClassOfService)
                .FirstOrDefaultAsync(up => up.IndexNumber == indexNumber && up.IsActive);

            // AirtimeAllowanceAmount represents the total monthly allowance (includes airtime AND data)
            // NULL or 0 means Unlimited
            var monthlyLimit = userPhone?.ClassOfService?.AirtimeAllowanceAmount;

            if (!monthlyLimit.HasValue || monthlyLimit.Value == 0)
                return false; // Unlimited means no overage

            return (monthlyUsage + callRecord.CallCostUSD) > monthlyLimit.Value;
        }

        public async Task<decimal> GetRemainingAllowanceAsync(string indexNumber, int month, int year)
        {
            var userPhone = await _context.UserPhones
                .Include(up => up.ClassOfService)
                .FirstOrDefaultAsync(up => up.IndexNumber == indexNumber && up.IsActive);

            // AirtimeAllowanceAmount represents the total monthly allowance (includes airtime AND data)
            // NULL or 0 means Unlimited
            var totalAllowance = userPhone?.ClassOfService?.AirtimeAllowanceAmount;

            if (!totalAllowance.HasValue || totalAllowance.Value == 0)
                return decimal.MaxValue; // Unlimited - return max value to indicate unlimited remaining

            var usage = await GetMonthlyUsageAsync(indexNumber, month, year);

            return Math.Max(0, totalAllowance.Value - usage);
        }

        public async Task<decimal> GetMonthlyUsageAsync(string indexNumber, int month, int year)
        {
            // Include calls where user is responsible OR calls assigned to them (accepted)
            return await _context.CallRecords
                .Where(c => (c.ResponsibleIndexNumber == indexNumber ||
                            (c.PayingIndexNumber == indexNumber && c.AssignmentStatus == "Accepted"))
                    && c.CallMonth == month
                    && c.CallYear == year
                    && c.VerificationType == VerificationType.Official.ToString())
                .SumAsync(c => c.CallCostUSD);
        }

        // ===================================================================
        // Payment Assignment Operations
        // ===================================================================

        public async Task<CallLogPaymentAssignment> AssignPaymentAsync(
            int callRecordId,
            string assignedFrom,
            string assignedTo,
            string reason)
        {
            try
            {
                var callRecord = await _context.CallRecords
                    .Include(c => c.ResponsibleUser)
                    .Include(c => c.UserPhone)
                    .FirstOrDefaultAsync(c => c.Id == callRecordId);

                if (callRecord == null)
                    throw new ArgumentException($"Call record {callRecordId} not found");

                if (callRecord.ResponsibleIndexNumber != assignedFrom)
                    throw new UnauthorizedAccessException("You can only assign your own call records");

                // Get assigned-to user for logging
                var assignedToUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.IndexNumber == assignedTo);
                if (assignedToUser == null)
                    throw new ArgumentException($"User with index number {assignedTo} not found");

                var assignment = new CallLogPaymentAssignment
                {
                    CallRecordId = callRecordId,
                    AssignedFrom = assignedFrom,
                    AssignedTo = assignedTo,
                    AssignmentReason = reason,
                    AssignedDate = DateTime.UtcNow,
                    AssignmentStatus = "Pending",
                    CreatedDate = DateTime.UtcNow
                };

                await _context.CallLogPaymentAssignments.AddAsync(assignment);

                // Save to get the assignment ID
                await _context.SaveChangesAsync();

                // Store old status for audit
                string oldStatus = callRecord.AssignmentStatus;

                // Update call record with the assignment ID (now available after save)
                callRecord.PayingIndexNumber = assignedTo;
                callRecord.PaymentAssignmentId = assignment.Id;
                callRecord.AssignmentStatus = "Pending";

                await _context.SaveChangesAsync();

                // Log audit trail
                await _auditLogService.LogCallPaymentAssignedAsync(
                    callRecordId,
                    assignedFrom,
                    $"{callRecord.ResponsibleUser?.FirstName} {callRecord.ResponsibleUser?.LastName}",
                    assignedTo,
                    $"{assignedToUser.FirstName} {assignedToUser.LastName}",
                    reason,
                    callRecord.CallCostUSD,
                    assignedFrom);

                // Send notification to assigned user
                await _notificationService.NotifyNewPaymentAssignmentAsync(
                    assignment.Id,
                    assignedToUser.ApplicationUserId ?? "",
                    $"{callRecord.ResponsibleUser?.FirstName} {callRecord.ResponsibleUser?.LastName}",
                    callRecord.UserPhone?.PhoneNumber ?? "N/A"
                );

                _logger.LogInformation("Payment assigned from {From} to {To} for call record {CallRecordId}",
                    assignedFrom, assignedTo, callRecordId);

                return assignment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning payment for call record {CallRecordId}", callRecordId);
                throw;
            }
        }

        public async Task<bool> AcceptPaymentAssignmentAsync(int assignmentId, string indexNumber)
        {
            try
            {
                // First check if assignment exists at all
                var assignment = await _context.CallLogPaymentAssignments
                    .Include(a => a.CallRecord)
                    .FirstOrDefaultAsync(a => a.Id == assignmentId);

                if (assignment == null)
                {
                    _logger.LogWarning("Assignment {AssignmentId} not found", assignmentId);
                    return false;
                }

                // Check if assigned to this user
                if (assignment.AssignedTo != indexNumber)
                {
                    _logger.LogWarning("Assignment {AssignmentId} is assigned to {AssignedTo}, not {IndexNumber}",
                        assignmentId, assignment.AssignedTo, indexNumber);
                    return false;
                }

                // Check if already processed
                if (assignment.AssignmentStatus != "Pending")
                {
                    _logger.LogWarning("Assignment {AssignmentId} already processed with status {Status}",
                        assignmentId, assignment.AssignmentStatus);
                    return false;
                }

                var acceptedByUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.IndexNumber == indexNumber);
                if (acceptedByUser == null)
                    return false;

                assignment.AssignmentStatus = "Accepted";
                assignment.AcceptedDate = DateTime.UtcNow;
                assignment.ModifiedDate = DateTime.UtcNow;

                // Update call record assignment status
                if (assignment.CallRecord != null)
                {
                    assignment.CallRecord.AssignmentStatus = "Accepted";
                }

                await _context.SaveChangesAsync();

                // Log audit trail
                await _auditLogService.LogCallPaymentAcceptedAsync(
                    assignment.CallRecordId,
                    assignmentId,
                    indexNumber,
                    $"{acceptedByUser.FirstName} {acceptedByUser.LastName}",
                    indexNumber);

                // Send notification to the original user (assignedFrom) that assignment was accepted
                var assignedFromUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.IndexNumber == assignment.AssignedFrom);
                if (assignedFromUser?.ApplicationUserId != null)
                {
                    await _notificationService.NotifyPaymentAssignmentAcceptedAsync(
                        assignmentId,
                        assignedFromUser.ApplicationUserId,
                        $"{acceptedByUser.FirstName} {acceptedByUser.LastName}"
                    );
                }

                _logger.LogInformation("Payment assignment {AssignmentId} accepted by {IndexNumber}",
                    assignmentId, indexNumber);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting payment assignment {AssignmentId}", assignmentId);
                return false;
            }
        }

        public async Task<bool> RejectPaymentAssignmentAsync(int assignmentId, string indexNumber, string reason)
        {
            try
            {
                // First check if assignment exists at all
                var assignment = await _context.CallLogPaymentAssignments
                    .Include(a => a.CallRecord)
                    .FirstOrDefaultAsync(a => a.Id == assignmentId);

                if (assignment == null)
                {
                    _logger.LogWarning("Assignment {AssignmentId} not found", assignmentId);
                    return false;
                }

                // Check if assigned to this user
                if (assignment.AssignedTo != indexNumber)
                {
                    _logger.LogWarning("Assignment {AssignmentId} is assigned to {AssignedTo}, not {IndexNumber}",
                        assignmentId, assignment.AssignedTo, indexNumber);
                    return false;
                }

                // Check if already processed
                if (assignment.AssignmentStatus != "Pending")
                {
                    _logger.LogWarning("Assignment {AssignmentId} already processed with status {Status}",
                        assignmentId, assignment.AssignmentStatus);
                    return false;
                }

                var rejectedByUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.IndexNumber == indexNumber);
                if (rejectedByUser == null)
                    return false;

                assignment.AssignmentStatus = "Rejected";
                assignment.RejectionReason = reason;
                assignment.ModifiedDate = DateTime.UtcNow;

                // Revert payment back to original user
                if (assignment.CallRecord != null)
                {
                    assignment.CallRecord.PayingIndexNumber = assignment.AssignedFrom;
                    assignment.CallRecord.PaymentAssignmentId = null;
                    assignment.CallRecord.AssignmentStatus = "None"; // Back to original owner
                }

                await _context.SaveChangesAsync();

                // Log audit trail
                await _auditLogService.LogCallPaymentRejectedAsync(
                    assignment.CallRecordId,
                    assignmentId,
                    indexNumber,
                    $"{rejectedByUser.FirstName} {rejectedByUser.LastName}",
                    reason,
                    indexNumber);

                // Send notification to the original user (assignedFrom) that assignment was rejected
                var assignedFromUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.IndexNumber == assignment.AssignedFrom);
                if (assignedFromUser?.ApplicationUserId != null)
                {
                    await _notificationService.NotifyPaymentAssignmentRejectedAsync(
                        assignmentId,
                        assignedFromUser.ApplicationUserId,
                        $"{rejectedByUser.FirstName} {rejectedByUser.LastName}",
                        reason
                    );
                }

                _logger.LogInformation("Payment assignment {AssignmentId} rejected by {IndexNumber}",
                    assignmentId, indexNumber);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting payment assignment {AssignmentId}", assignmentId);
                return false;
            }
        }

        public async Task<List<CallLogPaymentAssignment>> GetPendingAssignmentsAsync(string indexNumber)
        {
            return await _context.CallLogPaymentAssignments
                .Include(a => a.CallRecord)
                .Where(a => a.AssignedTo == indexNumber && a.AssignmentStatus == "Pending")
                .OrderByDescending(a => a.AssignedDate)
                .ToListAsync();
        }

        public async Task<List<CallLogPaymentAssignment>> GetAssignmentHistoryAsync(int callRecordId)
        {
            return await _context.CallLogPaymentAssignments
                .Where(a => a.CallRecordId == callRecordId)
                .OrderBy(a => a.AssignedDate)
                .ToListAsync();
        }

        // ===================================================================
        // Supervisor Approval Operations
        // ===================================================================

        public async Task<int> SubmitToSupervisorAsync(List<int> verificationIds, string indexNumber)
        {
            try
            {
                var verifications = await _context.CallLogVerifications
                    .Where(v => verificationIds.Contains(v.Id) && v.VerifiedBy == indexNumber)
                    .ToListAsync();

                if (!verifications.Any())
                    return 0;

                // Get supervisor from user record
                var user = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.IndexNumber == indexNumber);

                if (user == null || string.IsNullOrEmpty(user.SupervisorIndexNumber))
                    throw new InvalidOperationException("Supervisor not found for user");

                foreach (var verification in verifications)
                {
                    verification.SubmittedToSupervisor = true;
                    verification.SubmittedDate = DateTime.UtcNow;
                    verification.SupervisorIndexNumber = user.SupervisorIndexNumber;
                    verification.ModifiedDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Send notifications
                var totalAmount = verifications.Sum(v => v.ActualAmount);
                var callCount = verifications.Count;

                // Get user's application user ID
                var requesterAppUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == user.Email);

                // Get supervisor's application user ID
                var supervisorEbillUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.IndexNumber == user.SupervisorIndexNumber);

                if (requesterAppUser != null && supervisorEbillUser?.ApplicationUserId != null)
                {
                    // Use the first verification ID as representative
                    await _notificationService.NotifyCallLogVerificationSubmittedAsync(
                        verifications.First().Id,
                        requesterAppUser.Id,
                        supervisorEbillUser.ApplicationUserId,
                        callCount,
                        totalAmount
                    );

                    // Log audit trail
                    await _auditLogService.LogCallLogVerificationSubmittedAsync(
                        verifications.First().Id,
                        $"{user.FirstName} {user.LastName}",
                        indexNumber,
                        callCount,
                        totalAmount,
                        requesterAppUser.Id,
                        null
                    );
                }

                _logger.LogInformation("{Count} verifications submitted to supervisor by {IndexNumber}",
                    verifications.Count, indexNumber);

                return verifications.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting verifications to supervisor for user {IndexNumber}", indexNumber);
                throw;
            }
        }

        public async Task<List<CallLogVerification>> GetSupervisorPendingApprovalsAsync(string supervisorIndexNumber)
        {
            return await _context.CallLogVerifications
                .Include(v => v.CallRecord)
                .Include(v => v.Documents)
                .Where(v => v.SupervisorIndexNumber == supervisorIndexNumber
                    && v.SubmittedToSupervisor
                    && v.ApprovalStatus == "Pending")
                .OrderBy(v => v.SubmittedDate)
                .ToListAsync();
        }

        public async Task<bool> ApproveVerificationAsync(
            int verificationId,
            string supervisorIndexNumber,
            decimal? approvedAmount = null,
            string? comments = null)
        {
            try
            {
                var verification = await _context.CallLogVerifications
                    .Include(v => v.CallRecord)
                    .FirstOrDefaultAsync(v => v.Id == verificationId && v.SupervisorIndexNumber == supervisorIndexNumber);

                if (verification == null)
                    return false;

                verification.ApprovalStatus = approvedAmount.HasValue && approvedAmount.Value < verification.ActualAmount
                    ? "PartiallyApproved"
                    : "Approved";
                verification.SupervisorApprovalStatus = approvedAmount.HasValue && approvedAmount.Value < verification.ActualAmount
                    ? "PartiallyApproved"
                    : "Approved";
                verification.SupervisorApprovedBy = supervisorIndexNumber;
                verification.SupervisorApprovedDate = DateTime.UtcNow;
                verification.SupervisorComments = comments;
                verification.ApprovedAmount = approvedAmount ?? verification.ActualAmount;
                verification.ModifiedDate = DateTime.UtcNow;

                // Explicitly mark CallLogVerification as modified to ensure EF tracks all changes
                _context.Entry(verification).State = Microsoft.EntityFrameworkCore.EntityState.Modified;

                // Update call record
                if (verification.CallRecord != null)
                {
                    verification.CallRecord.SupervisorApprovalStatus = verification.ApprovalStatus;
                    verification.CallRecord.SupervisorApprovedBy = supervisorIndexNumber;
                    verification.CallRecord.SupervisorApprovedDate = DateTime.UtcNow;

                    // Explicitly mark CallRecord as modified to ensure EF tracks the changes
                    _context.Entry(verification.CallRecord).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Verification {Id} approved by supervisor {Supervisor}",
                    verificationId, supervisorIndexNumber);

                // Get user ID from EbillUser
                var ebillUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.IndexNumber == verification.VerifiedBy);

                if (ebillUser != null)
                {
                    var appUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == ebillUser.Email);

                    if (appUser != null)
                    {
                        // Create notification for user with detailed info
                        await _notificationService.NotifyCallLogVerificationApprovedAsync(
                            verificationId,
                            appUser.Id,
                            1, // Single call verification
                            verification.ActualAmount,
                            comments
                        );

                        // Log audit trail
                        var supervisorEbillUser = await _context.EbillUsers
                            .FirstOrDefaultAsync(u => u.IndexNumber == supervisorIndexNumber);

                        if (supervisorEbillUser != null && supervisorEbillUser.ApplicationUserId != null)
                        {
                            await _auditLogService.LogCallLogVerificationApprovedAsync(
                                verificationId,
                                $"{supervisorEbillUser.FirstName} {supervisorEbillUser.LastName}",
                                $"{ebillUser.FirstName} {ebillUser.LastName}",
                                1,
                                verification.ActualAmount,
                                comments,
                                supervisorEbillUser.ApplicationUserId,
                                null
                            );
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving verification {Id}", verificationId);
                return false;
            }
        }

        public async Task<bool> RejectVerificationAsync(
            int verificationId,
            string supervisorIndexNumber,
            string reason)
        {
            try
            {
                var verification = await _context.CallLogVerifications
                    .Include(v => v.CallRecord)
                    .FirstOrDefaultAsync(v => v.Id == verificationId && v.SupervisorIndexNumber == supervisorIndexNumber);

                if (verification == null)
                    return false;

                verification.ApprovalStatus = "Rejected";
                verification.SupervisorApprovalStatus = "Rejected";
                verification.SupervisorApprovedBy = supervisorIndexNumber;
                verification.SupervisorApprovedDate = DateTime.UtcNow;
                verification.RejectionReason = reason;
                verification.ModifiedDate = DateTime.UtcNow;

                // Explicitly mark CallLogVerification as modified to ensure EF tracks all changes
                _context.Entry(verification).State = Microsoft.EntityFrameworkCore.EntityState.Modified;

                // Update call record
                if (verification.CallRecord != null)
                {
                    verification.CallRecord.SupervisorApprovalStatus = "Rejected";
                    verification.CallRecord.SupervisorApprovedBy = supervisorIndexNumber;
                    verification.CallRecord.SupervisorApprovedDate = DateTime.UtcNow;

                    // Explicitly mark CallRecord as modified to ensure EF tracks the changes
                    _context.Entry(verification.CallRecord).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Verification {Id} rejected by supervisor {Supervisor}",
                    verificationId, supervisorIndexNumber);

                // Get user ID from EbillUser
                var ebillUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.IndexNumber == verification.VerifiedBy);

                if (ebillUser != null)
                {
                    var appUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == ebillUser.Email);

                    if (appUser != null)
                    {
                        // Create rejection notification for user with detailed info
                        await _notificationService.NotifyCallLogVerificationRejectedAsync(
                            verificationId,
                            appUser.Id,
                            1, // Single call verification
                            verification.ActualAmount,
                            reason
                        );

                        // Log audit trail
                        var supervisorEbillUser = await _context.EbillUsers
                            .FirstOrDefaultAsync(u => u.IndexNumber == supervisorIndexNumber);

                        if (supervisorEbillUser != null && supervisorEbillUser.ApplicationUserId != null)
                        {
                            await _auditLogService.LogCallLogVerificationRejectedAsync(
                                verificationId,
                                $"{supervisorEbillUser.FirstName} {supervisorEbillUser.LastName}",
                                $"{ebillUser.FirstName} {ebillUser.LastName}",
                                1,
                                verification.ActualAmount,
                                reason,
                                supervisorEbillUser.ApplicationUserId,
                                null
                            );
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting verification {Id}", verificationId);
                return false;
            }
        }

        public async Task<bool> RevertVerificationAsync(
            int verificationId,
            string supervisorIndexNumber,
            string reason)
        {
            try
            {
                var verification = await _context.CallLogVerifications
                    .Include(v => v.CallRecord)
                    .FirstOrDefaultAsync(v => v.Id == verificationId && v.SupervisorIndexNumber == supervisorIndexNumber);

                if (verification == null)
                    return false;

                verification.ApprovalStatus = "Reverted";
                verification.SupervisorApprovalStatus = "Reverted";
                verification.SupervisorApprovedBy = supervisorIndexNumber;
                verification.SupervisorApprovedDate = DateTime.UtcNow;
                verification.SupervisorComments = reason;
                verification.SubmittedToSupervisor = false; // Allow user to resubmit
                verification.ModifiedDate = DateTime.UtcNow;

                // Explicitly mark CallLogVerification as modified to ensure EF tracks all changes
                _context.Entry(verification).State = Microsoft.EntityFrameworkCore.EntityState.Modified;

                // Update call record
                if (verification.CallRecord != null)
                {
                    verification.CallRecord.SupervisorApprovalStatus = "Reverted";
                    verification.CallRecord.SupervisorApprovedBy = supervisorIndexNumber;
                    verification.CallRecord.SupervisorApprovedDate = DateTime.UtcNow;

                    // Explicitly mark CallRecord as modified to ensure EF tracks the changes
                    _context.Entry(verification.CallRecord).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Verification {Id} reverted by supervisor {Supervisor}",
                    verificationId, supervisorIndexNumber);

                // Get user ID from EbillUser
                var ebillUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.IndexNumber == verification.VerifiedBy);

                if (ebillUser != null)
                {
                    var appUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == ebillUser.Email);

                    if (appUser != null)
                    {
                        // Create revert notification for user
                        await _notificationService.CreateRevertNotificationAsync(
                            verificationId,
                            appUser.Id,
                            reason
                        );
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reverting verification {Id}", verificationId);
                return false;
            }
        }

        public async Task<int> BatchApproveVerificationsAsync(
            List<int> verificationIds,
            string supervisorIndexNumber)
        {
            int approvedCount = 0;

            foreach (var id in verificationIds)
            {
                if (await ApproveVerificationAsync(id, supervisorIndexNumber))
                    approvedCount++;
            }

            return approvedCount;
        }

        // ===================================================================
        // Reporting & Analytics
        // ===================================================================

        public async Task<VerificationSummary> GetVerificationSummaryAsync(
            string indexNumber,
            int month,
            int year)
        {
            var callRecords = await _context.CallRecords
                .Where(c => c.ResponsibleIndexNumber == indexNumber
                    && c.CallMonth == month
                    && c.CallYear == year)
                .ToListAsync();

            var verifications = await _context.CallLogVerifications
                .Where(v => v.VerifiedBy == indexNumber
                    && v.CallRecord.CallMonth == month
                    && v.CallRecord.CallYear == year)
                .Include(v => v.CallRecord)
                .ToListAsync();

            var userPhone = await _context.UserPhones
                .Include(up => up.ClassOfService)
                .FirstOrDefaultAsync(up => up.IndexNumber == indexNumber && up.IsActive);

            // AirtimeAllowanceAmount represents the total monthly allowance (includes airtime AND data)
            // NULL or 0 means Unlimited
            decimal allowanceLimit = userPhone?.ClassOfService?.AirtimeAllowanceAmount ?? 0;
            decimal totalUsage = callRecords.Where(c => c.VerificationType == VerificationType.Official.ToString())
                .Sum(c => c.CallCostUSD);

            var assignments = await _context.CallLogPaymentAssignments
                .Where(a => a.AssignedFrom == indexNumber || a.AssignedTo == indexNumber)
                .Include(a => a.CallRecord)
                .Where(a => a.CallRecord.CallMonth == month && a.CallRecord.CallYear == year)
                .ToListAsync();

            var summary = new VerificationSummary
            {
                IndexNumber = indexNumber,
                Month = month,
                Year = year,
                TotalCalls = callRecords.Count,
                VerifiedCalls = verifications.Count,
                UnverifiedCalls = callRecords.Count - verifications.Count,
                PersonalCalls = verifications.Count(v => v.VerificationType == VerificationType.Personal),
                OfficialCalls = verifications.Count(v => v.VerificationType == VerificationType.Official),
                TotalAmount = callRecords.Sum(c => c.CallCostUSD),
                VerifiedAmount = verifications.Sum(v => v.ActualAmount),
                PersonalAmount = verifications.Where(v => v.VerificationType == VerificationType.Personal)
                    .Sum(v => v.ActualAmount),
                OfficialAmount = verifications.Where(v => v.VerificationType == VerificationType.Official)
                    .Sum(v => v.ActualAmount),
                AllowanceLimit = allowanceLimit,
                TotalUsage = totalUsage,
                RemainingAllowance = Math.Max(0, allowanceLimit - totalUsage),
                IsOverAllowance = totalUsage > allowanceLimit,
                OverageAmount = Math.Max(0, totalUsage - allowanceLimit),
                PendingApproval = verifications.Count(v => v.ApprovalStatus == "Pending"),
                Approved = verifications.Count(v => v.ApprovalStatus == "Approved"),
                Rejected = verifications.Count(v => v.ApprovalStatus == "Rejected"),
                PartiallyApproved = verifications.Count(v => v.ApprovalStatus == "PartiallyApproved"),
                AssignedToOthers = assignments.Count(a => a.AssignedFrom == indexNumber),
                AssignedFromOthers = assignments.Count(a => a.AssignedTo == indexNumber),
                VerificationDeadline = callRecords.FirstOrDefault()?.VerificationPeriod,
                CompliancePercentage = callRecords.Count > 0
                    ? (decimal)verifications.Count / callRecords.Count * 100
                    : 0
            };

            if (summary.VerificationDeadline.HasValue)
            {
                summary.IsOverdue = DateTime.UtcNow > summary.VerificationDeadline.Value
                    && summary.UnverifiedCalls > 0;
            }

            return summary;
        }

        public async Task<decimal> GetVerificationComplianceRateAsync(int month, int year)
        {
            var totalCalls = await _context.CallRecords
                .Where(c => c.CallMonth == month && c.CallYear == year)
                .CountAsync();

            if (totalCalls == 0)
                return 100;

            var verifiedCalls = await _context.CallRecords
                .Where(c => c.CallMonth == month && c.CallYear == year && c.IsVerified)
                .CountAsync();

            return (decimal)verifiedCalls / totalCalls * 100;
        }

        public async Task<List<CallLogVerification>> GetOverdueVerificationsAsync()
        {
            var today = DateTime.UtcNow.Date;

            return await _context.CallLogVerifications
                .Include(v => v.CallRecord)
                .Where(v => v.CallRecord.VerificationPeriod.HasValue
                    && v.CallRecord.VerificationPeriod.Value < today
                    && !v.SubmittedToSupervisor)
                .OrderBy(v => v.CallRecord.VerificationPeriod)
                .ToListAsync();
        }
    }
}
