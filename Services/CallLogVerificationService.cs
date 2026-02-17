using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Models.Enums;

namespace TAB.Web.Services
{
    public class CallLogVerificationService : ICallLogVerificationService
    {
        private const string AutoOfficialCallTypePrefix = "Corporate Value Pack Data";

        private readonly ApplicationDbContext _context;
        private readonly ILogger<CallLogVerificationService> _logger;
        private readonly IAuditLogService _auditLogService;
        private readonly INotificationService _notificationService;
        private readonly IClassOfServiceCalculationService _classOfServiceCalculation;

        public CallLogVerificationService(
            ApplicationDbContext context,
            ILogger<CallLogVerificationService> logger,
            IAuditLogService auditLogService,
            INotificationService notificationService,
            IClassOfServiceCalculationService classOfServiceCalculation)
        {
            _context = context;
            _logger = logger;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
            _classOfServiceCalculation = classOfServiceCalculation;
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

                // Check if verification deadline has expired
                if (callRecord.VerificationPeriod.HasValue && callRecord.VerificationPeriod.Value < DateTime.UtcNow)
                {
                    throw new InvalidOperationException($"Verification deadline ({callRecord.VerificationPeriod.Value:MMM dd, yyyy HH:mm}) has expired. This call will be automatically recovered as Personal.");
                }

                // Check if already verified
                var existingVerification = await _context.CallLogVerifications
                    .FirstOrDefaultAsync(v => v.CallRecordId == callRecordId);

                // Allow changes if submitted but not yet approved by supervisor
                // Only block if supervisor has approved or partially approved
                if (existingVerification != null && existingVerification.SubmittedToSupervisor)
                {
                    var approvalStatus = existingVerification.ApprovalStatus;
                    if (approvalStatus == "Approved" || approvalStatus == "PartiallyApproved")
                    {
                        throw new InvalidOperationException("Cannot modify a verification that has been approved by supervisor");
                    }
                    // If status is Pending, Rejected, or Reverted, allow changes
                }

                // Get the SPECIFIC phone that made this call to check its allowance
                // This ensures overage is tracked per phone, not per person
                UserPhone? userPhone = null;
                if (callRecord.UserPhoneId.HasValue)
                {
                    userPhone = await _context.UserPhones
                        .Include(up => up.ClassOfService)
                        .FirstOrDefaultAsync(up => up.Id == callRecord.UserPhoneId.Value && up.IsActive);
                }

                // Block Personal verification for auto-official CallType
                if (callRecord.CallType != null && callRecord.CallType.StartsWith(AutoOfficialCallTypePrefix, StringComparison.OrdinalIgnoreCase) && verificationType == VerificationType.Personal)
                    throw new InvalidOperationException("Calls with type 'Corporate Value Pack Data' are auto-official and cannot be marked as Personal.");

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

                    // Check if this call puts THIS SPECIFIC PHONE over its allowance (only if limit is set)
                    if (allowanceLimit.HasValue && allowanceLimit.Value > 0 && callRecord.UserPhoneId.HasValue)
                    {
                        var monthlyUsage = await _classOfServiceCalculation.GetPhoneMonthlyUsageAsync(
                            callRecord.UserPhoneId.Value,
                            callRecord.CallMonth,
                            callRecord.CallYear);
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

                    // If this was previously submitted but not approved, clear the submission
                    // so staff can resubmit with the new changes
                    if (existingVerification.SubmittedToSupervisor)
                    {
                        var currentStatus = existingVerification.ApprovalStatus;
                        if (currentStatus != "Approved" && currentStatus != "PartiallyApproved")
                        {
                            existingVerification.SubmittedToSupervisor = false;
                            existingVerification.SubmittedDate = null;
                            existingVerification.ApprovalStatus = "Pending";

                            _logger.LogInformation(
                                "Verification {Id} was modified after submission. Clearing submission status - staff must resubmit.",
                                existingVerification.Id);
                        }
                    }

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

        /// <summary>
        /// Optimized bulk verification - processes all records in a single transaction
        /// with minimal database round trips.
        /// </summary>
        public async Task<BulkVerificationResult> BulkVerifyCallLogsAsync(
            List<int> callRecordIds,
            string indexNumber,
            VerificationType verificationType,
            string? justification = null)
        {
            var result = new BulkVerificationResult();

            if (callRecordIds == null || !callRecordIds.Any())
            {
                result.Errors.Add("No call record IDs provided");
                return result;
            }

            try
            {
                var now = DateTime.UtcNow;

                // OPTIMIZATION 1: Load all call records in a single query
                var callRecords = await _context.CallRecords
                    .Where(c => callRecordIds.Contains(c.Id))
                    .ToListAsync();

                if (!callRecords.Any())
                {
                    result.Errors.Add("No call records found for the provided IDs");
                    return result;
                }

                // OPTIMIZATION 2: Load all existing verifications in a single query
                var existingVerifications = await _context.CallLogVerifications
                    .Where(v => callRecordIds.Contains(v.CallRecordId))
                    .ToDictionaryAsync(v => v.CallRecordId, v => v);

                // OPTIMIZATION 3: Get assigned call IDs for this user in a single query
                var assignedCallIdsList = await _context.Set<CallLogPaymentAssignment>()
                    .Where(a => a.AssignedTo == indexNumber && a.AssignmentStatus == "Accepted")
                    .Select(a => a.CallRecordId)
                    .ToListAsync();
                var assignedCallIds = assignedCallIdsList.ToHashSet();

                var verificationsToAdd = new List<CallLogVerification>();
                var verificationsToUpdate = new List<CallLogVerification>();
                var callsToUpdate = new List<CallRecord>();

                foreach (var callRecord in callRecords)
                {
                    // Skip auto-official calls when verifying as Personal
                    if (verificationType == VerificationType.Personal && callRecord.CallType != null && callRecord.CallType.StartsWith(AutoOfficialCallTypePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        result.SkippedCount++;
                        continue;
                    }

                    // Permission check
                    bool isResponsibleUser = callRecord.ResponsibleIndexNumber == indexNumber;
                    bool isPayingUser = callRecord.PayingIndexNumber == indexNumber && callRecord.AssignmentStatus == "Accepted";
                    bool isAssignedUser = assignedCallIds.Contains(callRecord.Id);

                    if (!isResponsibleUser && !isPayingUser && !isAssignedUser)
                    {
                        result.UnauthorizedCount++;
                        continue;
                    }

                    // Check if call has been assigned out and is awaiting acceptance
                    if (callRecord.AssignmentStatus == "Pending" &&
                        callRecord.ResponsibleIndexNumber == indexNumber &&
                        callRecord.PayingIndexNumber != indexNumber)
                    {
                        result.SkippedCount++;
                        continue;
                    }

                    // Check if verification deadline has expired
                    if (callRecord.VerificationPeriod.HasValue && callRecord.VerificationPeriod.Value < now)
                    {
                        result.ExpiredCount++;
                        continue;
                    }

                    // Check if locked by supervisor approval
                    if (existingVerifications.TryGetValue(callRecord.Id, out var existingVerification))
                    {
                        if (existingVerification.SubmittedToSupervisor)
                        {
                            var approvalStatus = existingVerification.ApprovalStatus;
                            if (approvalStatus == "Approved" || approvalStatus == "PartiallyApproved")
                            {
                                result.LockedCount++;
                                continue;
                            }
                        }

                        // Update existing verification
                        existingVerification.VerificationType = verificationType;
                        existingVerification.JustificationText = justification ?? $"Bulk verified as {verificationType}";
                        existingVerification.ModifiedDate = now;

                        // Clear submission if previously submitted but not approved
                        if (existingVerification.SubmittedToSupervisor)
                        {
                            var currentStatus = existingVerification.ApprovalStatus;
                            if (currentStatus != "Approved" && currentStatus != "PartiallyApproved")
                            {
                                existingVerification.SubmittedToSupervisor = false;
                                existingVerification.SubmittedDate = null;
                                existingVerification.ApprovalStatus = "Pending";
                            }
                        }

                        verificationsToUpdate.Add(existingVerification);
                    }
                    else
                    {
                        // Create new verification
                        var newVerification = new CallLogVerification
                        {
                            CallRecordId = callRecord.Id,
                            VerifiedBy = indexNumber,
                            VerifiedDate = now,
                            VerificationType = verificationType,
                            ActualAmount = callRecord.CallCostUSD,
                            IsOverage = false, // Simplified for bulk - no overage check
                            JustificationText = justification ?? $"Bulk verified as {verificationType}",
                            ApprovalStatus = "Pending",
                            CreatedDate = now
                        };
                        verificationsToAdd.Add(newVerification);
                    }

                    // Update call record
                    callRecord.IsVerified = true;
                    callRecord.VerificationDate = now;
                    callRecord.VerificationType = verificationType.ToString();
                    callsToUpdate.Add(callRecord);

                    result.VerifiedCount++;
                }

                // OPTIMIZATION 4: Batch all changes and save in a single transaction
                if (verificationsToAdd.Any())
                {
                    await _context.CallLogVerifications.AddRangeAsync(verificationsToAdd);
                }

                if (verificationsToUpdate.Any())
                {
                    _context.CallLogVerifications.UpdateRange(verificationsToUpdate);
                }

                if (callsToUpdate.Any())
                {
                    _context.CallRecords.UpdateRange(callsToUpdate);
                }

                // Single SaveChanges for all operations
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Bulk verification completed: {VerifiedCount} verified, {SkippedCount} skipped, {LockedCount} locked, {ExpiredCount} expired, {UnauthorizedCount} unauthorized",
                    result.VerifiedCount, result.SkippedCount, result.LockedCount, result.ExpiredCount, result.UnauthorizedCount);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk verification for user {IndexNumber}", indexNumber);
                result.Errors.Add($"Database error: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// ULTRA-FAST bulk verification using raw SQL.
        /// Executes directly on database - can verify thousands of records in milliseconds.
        /// </summary>
        public async Task<BulkVerificationResult> BulkVerifyByExtensionMonthRawAsync(
            string extension,
            int month,
            int year,
            string indexNumber,
            VerificationType verificationType,
            string? justification = null)
        {
            var result = new BulkVerificationResult();
            var now = DateTime.UtcNow;
            var verificationTypeStr = verificationType.ToString();
            var justificationText = justification ?? $"Bulk verified as {verificationType}";

            try
            {
                // DIAGNOSTIC: First, count how many records SHOULD match (using same logic as page)
                var diagnosticCount = await _context.CallRecords
                    .Include(c => c.UserPhone)
                    .Where(c => (c.UserPhone != null ? c.UserPhone.PhoneNumber : "Unknown") == extension
                        && c.CallMonth == month
                        && c.CallYear == year
                        && c.ResponsibleIndexNumber == indexNumber)
                    .CountAsync();

                _logger.LogInformation(
                    "DIAGNOSTIC: Found {Count} total records matching extension={Extension}, month={Month}, year={Year}, indexNumber={IndexNumber}",
                    diagnosticCount, extension, month, year, indexNumber);

                // Also check without ResponsibleIndexNumber filter to see if that's the issue
                var countWithoutIndexFilter = await _context.CallRecords
                    .Include(c => c.UserPhone)
                    .Where(c => (c.UserPhone != null ? c.UserPhone.PhoneNumber : "Unknown") == extension
                        && c.CallMonth == month
                        && c.CallYear == year)
                    .CountAsync();

                _logger.LogInformation(
                    "DIAGNOSTIC: Found {Count} records WITHOUT indexNumber filter (extension={Extension}, month={Month}, year={Year})",
                    countWithoutIndexFilter, extension, month, year);

                // Auto-official exclusion: skip auto-official calls when verifying as Personal
                var autoOfficialExclusion = verificationType == VerificationType.Personal
                    ? $" AND ISNULL(cr.call_type, '') NOT LIKE '{AutoOfficialCallTypePrefix}%'"
                    : "";

                // Use execution strategy to handle retries with transactions
                var strategy = _context.Database.CreateExecutionStrategy();
                var callRecordsUpdated = 0;
                var verificationTypeInt = (int)verificationType;

                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    // Step 1: Update CallRecords directly with raw SQL
                    // JOIN with UserPhones because extension in UI comes from UserPhones.PhoneNumber
                    // Handle 'Unknown' case: when UserPhone is NULL, the UI shows 'Unknown'
                    // NOTE: Using actual database column names (from [Column] attributes), not C# property names!
                    var updateCallRecordsSql = $@"
                        UPDATE cr
                        SET cr.call_ver_ind = 1,
                            cr.call_ver_date = @now,
                            cr.verification_type = @verificationType
                        FROM CallRecords cr
                        LEFT JOIN UserPhones up ON cr.UserPhoneId = up.Id
                        WHERE (
                            (@extension = 'Unknown' AND cr.UserPhoneId IS NULL)
                            OR up.PhoneNumber = @extension
                            OR cr.ext_no = @extension
                        )
                          AND cr.call_month = @month
                          AND cr.call_year = @year
                          AND cr.ext_resp_index = @indexNumber
                          AND (cr.supervisor_approval_status IS NULL OR cr.supervisor_approval_status = '' OR cr.supervisor_approval_status = 'Pending')
                          AND (cr.verification_period IS NULL OR cr.verification_period > @now){autoOfficialExclusion}";

                    callRecordsUpdated = await _context.Database.ExecuteSqlRawAsync(
                        updateCallRecordsSql,
                        new Microsoft.Data.SqlClient.SqlParameter("@now", now),
                        new Microsoft.Data.SqlClient.SqlParameter("@verificationType", verificationTypeStr),
                        new Microsoft.Data.SqlClient.SqlParameter("@extension", extension),
                        new Microsoft.Data.SqlClient.SqlParameter("@month", month),
                        new Microsoft.Data.SqlClient.SqlParameter("@year", year),
                        new Microsoft.Data.SqlClient.SqlParameter("@indexNumber", indexNumber));

                    // Step 2: Update existing CallLogVerifications
                    // CallLogVerifications table uses property names (no [Column] attributes)
                    // But CallRecords join uses actual database column names
                    var updateVerificationsSql = $@"
                        UPDATE clv
                        SET clv.VerificationType = @verificationTypeInt,
                            clv.JustificationText = @justification,
                            clv.ModifiedDate = @now,
                            clv.SubmittedToSupervisor = CASE
                                WHEN clv.ApprovalStatus IN ('Approved', 'PartiallyApproved') THEN clv.SubmittedToSupervisor
                                ELSE 0
                            END,
                            clv.ApprovalStatus = CASE
                                WHEN clv.ApprovalStatus IN ('Approved', 'PartiallyApproved') THEN clv.ApprovalStatus
                                ELSE 'Pending'
                            END
                        FROM CallLogVerifications clv
                        INNER JOIN CallRecords cr ON clv.CallRecordId = cr.Id
                        LEFT JOIN UserPhones up ON cr.UserPhoneId = up.Id
                        WHERE (
                            (@extension = 'Unknown' AND cr.UserPhoneId IS NULL)
                            OR up.PhoneNumber = @extension
                            OR cr.ext_no = @extension
                        )
                          AND cr.call_month = @month
                          AND cr.call_year = @year
                          AND cr.ext_resp_index = @indexNumber
                          AND (cr.supervisor_approval_status IS NULL OR cr.supervisor_approval_status = '' OR cr.supervisor_approval_status = 'Pending'){autoOfficialExclusion}";

                    await _context.Database.ExecuteSqlRawAsync(
                        updateVerificationsSql,
                        new Microsoft.Data.SqlClient.SqlParameter("@verificationTypeInt", verificationTypeInt),
                        new Microsoft.Data.SqlClient.SqlParameter("@justification", justificationText),
                        new Microsoft.Data.SqlClient.SqlParameter("@now", now),
                        new Microsoft.Data.SqlClient.SqlParameter("@extension", extension),
                        new Microsoft.Data.SqlClient.SqlParameter("@month", month),
                        new Microsoft.Data.SqlClient.SqlParameter("@year", year),
                        new Microsoft.Data.SqlClient.SqlParameter("@indexNumber", indexNumber));

                    // Step 3: Insert new CallLogVerifications for records that don't have one
                    // CallLogVerifications columns use property names, CallRecords columns use database names
                    var insertVerificationsSql = $@"
                        INSERT INTO CallLogVerifications (CallRecordId, VerifiedBy, VerifiedDate, VerificationType,
                            ActualAmount, IsOverage, JustificationText, ApprovalStatus, CreatedDate, SubmittedToSupervisor)
                        SELECT cr.Id, @indexNumber, @now, @verificationTypeInt,
                               cr.call_cost_usd, 0, @justification, 'Pending', @now, 0
                        FROM CallRecords cr
                        LEFT JOIN UserPhones up ON cr.UserPhoneId = up.Id
                        WHERE (
                            (@extension = 'Unknown' AND cr.UserPhoneId IS NULL)
                            OR up.PhoneNumber = @extension
                            OR cr.ext_no = @extension
                        )
                          AND cr.call_month = @month
                          AND cr.call_year = @year
                          AND cr.ext_resp_index = @indexNumber
                          AND (cr.supervisor_approval_status IS NULL OR cr.supervisor_approval_status = '' OR cr.supervisor_approval_status = 'Pending')
                          AND (cr.verification_period IS NULL OR cr.verification_period > @now)
                          AND NOT EXISTS (SELECT 1 FROM CallLogVerifications clv WHERE clv.CallRecordId = cr.Id){autoOfficialExclusion}";

                    await _context.Database.ExecuteSqlRawAsync(
                        insertVerificationsSql,
                        new Microsoft.Data.SqlClient.SqlParameter("@indexNumber", indexNumber),
                        new Microsoft.Data.SqlClient.SqlParameter("@now", now),
                        new Microsoft.Data.SqlClient.SqlParameter("@verificationTypeInt", verificationTypeInt),
                        new Microsoft.Data.SqlClient.SqlParameter("@justification", justificationText),
                        new Microsoft.Data.SqlClient.SqlParameter("@extension", extension),
                        new Microsoft.Data.SqlClient.SqlParameter("@month", month),
                        new Microsoft.Data.SqlClient.SqlParameter("@year", year));

                    await transaction.CommitAsync();
                });

                result.VerifiedCount = callRecordsUpdated;

                _logger.LogInformation(
                    "RAW SQL bulk verification completed for {Extension}/{Month}/{Year}: {Count} records in single transaction",
                    extension, month, year, callRecordsUpdated);

                // If raw SQL found 0 but diagnostic found records, fall back to EF Core method
                if (callRecordsUpdated == 0 && diagnosticCount > 0)
                {
                    _logger.LogWarning(
                        "RAW SQL found 0 records but diagnostic found {DiagnosticCount}. Falling back to EF Core method.",
                        diagnosticCount);

                    // Get the call record IDs using EF Core (same logic as page)
                    var callRecordIds = await _context.CallRecords
                        .Include(c => c.UserPhone)
                        .Where(c => (c.UserPhone != null ? c.UserPhone.PhoneNumber : "Unknown") == extension
                            && c.CallMonth == month
                            && c.CallYear == year
                            && c.ResponsibleIndexNumber == indexNumber)
                        .Select(c => c.Id)
                        .ToListAsync();

                    if (callRecordIds.Any())
                    {
                        // Use the existing BulkVerifyCallLogsAsync method as fallback
                        return await BulkVerifyCallLogsAsync(callRecordIds, indexNumber, verificationType, justificationText);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during raw SQL bulk verification for {Extension}/{Month}/{Year}", extension, month, year);
                result.Errors.Add($"Database error: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// ULTRA-FAST bulk verification by dialed number using raw SQL.
        /// </summary>
        public async Task<BulkVerificationResult> BulkVerifyByDialedNumberRawAsync(
            string extension,
            int month,
            int year,
            string dialedNumber,
            string indexNumber,
            VerificationType verificationType,
            string? justification = null)
        {
            var result = new BulkVerificationResult();
            var now = DateTime.UtcNow;
            var verificationTypeStr = verificationType.ToString();
            var justificationText = justification ?? $"Bulk verified as {verificationType}";

            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                var callRecordsUpdated = 0;
                var verificationTypeInt = (int)verificationType;

                // Auto-official exclusion: skip auto-official calls when verifying as Personal
                var autoOfficialExclusion = verificationType == VerificationType.Personal
                    ? $" AND ISNULL(cr.call_type, '') NOT LIKE '{AutoOfficialCallTypePrefix}%'"
                    : "";

                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    // Step 1: Update CallRecords
                    // NOTE: Using actual database column names (from [Column] attributes)
                    var updateCallRecordsSql = $@"
                        UPDATE cr
                        SET cr.call_ver_ind = 1,
                            cr.call_ver_date = @now,
                            cr.verification_type = @verificationType
                        FROM CallRecords cr
                        LEFT JOIN UserPhones up ON cr.UserPhoneId = up.Id
                        WHERE (up.PhoneNumber = @extension OR cr.ext_no = @extension)
                          AND cr.call_month = @month
                          AND cr.call_year = @year
                          AND ISNULL(cr.call_number, 'Unknown') = @dialedNumber
                          AND cr.ext_resp_index = @indexNumber
                          AND (cr.supervisor_approval_status IS NULL OR cr.supervisor_approval_status = '' OR cr.supervisor_approval_status = 'Pending')
                          AND (cr.verification_period IS NULL OR cr.verification_period > @now){autoOfficialExclusion}";

                    callRecordsUpdated = await _context.Database.ExecuteSqlRawAsync(
                        updateCallRecordsSql,
                        new Microsoft.Data.SqlClient.SqlParameter("@now", now),
                        new Microsoft.Data.SqlClient.SqlParameter("@verificationType", verificationTypeStr),
                        new Microsoft.Data.SqlClient.SqlParameter("@extension", extension),
                        new Microsoft.Data.SqlClient.SqlParameter("@month", month),
                        new Microsoft.Data.SqlClient.SqlParameter("@year", year),
                        new Microsoft.Data.SqlClient.SqlParameter("@dialedNumber", dialedNumber),
                        new Microsoft.Data.SqlClient.SqlParameter("@indexNumber", indexNumber));

                    // Step 2: Update existing verifications
                    var updateVerificationsSql = $@"
                        UPDATE clv
                        SET clv.VerificationType = @verificationTypeInt,
                            clv.JustificationText = @justification,
                            clv.ModifiedDate = @now,
                            clv.SubmittedToSupervisor = CASE
                                WHEN clv.ApprovalStatus IN ('Approved', 'PartiallyApproved') THEN clv.SubmittedToSupervisor
                                ELSE 0
                            END,
                            clv.ApprovalStatus = CASE
                                WHEN clv.ApprovalStatus IN ('Approved', 'PartiallyApproved') THEN clv.ApprovalStatus
                                ELSE 'Pending'
                            END
                        FROM CallLogVerifications clv
                        INNER JOIN CallRecords cr ON clv.CallRecordId = cr.Id
                        LEFT JOIN UserPhones up ON cr.UserPhoneId = up.Id
                        WHERE (up.PhoneNumber = @extension OR cr.ext_no = @extension)
                          AND cr.call_month = @month
                          AND cr.call_year = @year
                          AND ISNULL(cr.call_number, 'Unknown') = @dialedNumber
                          AND cr.ext_resp_index = @indexNumber{autoOfficialExclusion}";

                    await _context.Database.ExecuteSqlRawAsync(
                        updateVerificationsSql,
                        new Microsoft.Data.SqlClient.SqlParameter("@verificationTypeInt", verificationTypeInt),
                        new Microsoft.Data.SqlClient.SqlParameter("@justification", justificationText),
                        new Microsoft.Data.SqlClient.SqlParameter("@now", now),
                        new Microsoft.Data.SqlClient.SqlParameter("@extension", extension),
                        new Microsoft.Data.SqlClient.SqlParameter("@month", month),
                        new Microsoft.Data.SqlClient.SqlParameter("@year", year),
                        new Microsoft.Data.SqlClient.SqlParameter("@dialedNumber", dialedNumber),
                        new Microsoft.Data.SqlClient.SqlParameter("@indexNumber", indexNumber));

                    // Step 3: Insert new verifications
                    var insertVerificationsSql = $@"
                        INSERT INTO CallLogVerifications (CallRecordId, VerifiedBy, VerifiedDate, VerificationType,
                            ActualAmount, IsOverage, JustificationText, ApprovalStatus, CreatedDate, SubmittedToSupervisor)
                        SELECT cr.Id, @indexNumber, @now, @verificationTypeInt,
                               cr.call_cost_usd, 0, @justification, 'Pending', @now, 0
                        FROM CallRecords cr
                        LEFT JOIN UserPhones up ON cr.UserPhoneId = up.Id
                        WHERE (up.PhoneNumber = @extension OR cr.ext_no = @extension)
                          AND cr.call_month = @month
                          AND cr.call_year = @year
                          AND ISNULL(cr.call_number, 'Unknown') = @dialedNumber
                          AND cr.ext_resp_index = @indexNumber
                          AND (cr.supervisor_approval_status IS NULL OR cr.supervisor_approval_status = '' OR cr.supervisor_approval_status = 'Pending')
                          AND (cr.verification_period IS NULL OR cr.verification_period > @now)
                          AND NOT EXISTS (SELECT 1 FROM CallLogVerifications clv WHERE clv.CallRecordId = cr.Id){autoOfficialExclusion}";

                    await _context.Database.ExecuteSqlRawAsync(
                        insertVerificationsSql,
                        new Microsoft.Data.SqlClient.SqlParameter("@indexNumber", indexNumber),
                        new Microsoft.Data.SqlClient.SqlParameter("@now", now),
                        new Microsoft.Data.SqlClient.SqlParameter("@verificationTypeInt", verificationTypeInt),
                        new Microsoft.Data.SqlClient.SqlParameter("@justification", justificationText),
                        new Microsoft.Data.SqlClient.SqlParameter("@extension", extension),
                        new Microsoft.Data.SqlClient.SqlParameter("@month", month),
                        new Microsoft.Data.SqlClient.SqlParameter("@year", year),
                        new Microsoft.Data.SqlClient.SqlParameter("@dialedNumber", dialedNumber));

                    await transaction.CommitAsync();
                });

                result.VerifiedCount = callRecordsUpdated;

                _logger.LogInformation(
                    "RAW SQL bulk verification completed for dialed number {DialedNumber}: {Count} records",
                    dialedNumber, callRecordsUpdated);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during raw SQL bulk verification for dialed number {DialedNumber}", dialedNumber);
                result.Errors.Add($"Database error: {ex.Message}");
                return result;
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

        /// <summary>
        /// ULTRA-FAST bulk reassignment using raw SQL for an entire extension/month.
        /// </summary>
        public async Task<BulkReassignmentResult> BulkReassignByExtensionMonthRawAsync(
            string extension,
            int month,
            int year,
            string assignedFrom,
            string assignedTo,
            string reason)
        {
            var result = new BulkReassignmentResult();
            var now = DateTime.UtcNow;

            try
            {
                // Validate assigned-to user exists
                var assignedToUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.IndexNumber == assignedTo);
                if (assignedToUser == null)
                    throw new ArgumentException($"User with index number {assignedTo} not found");

                var strategy = _context.Database.CreateExecutionStrategy();
                var assignmentsInserted = 0;

                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    // Step 1: Get IDs of eligible calls (not submitted to supervisor)
                    var getEligibleCallsSql = @"
                        SELECT cr.Id
                        FROM CallRecords cr
                        LEFT JOIN UserPhones up ON cr.UserPhoneId = up.Id
                        LEFT JOIN CallLogVerifications clv ON clv.CallRecordId = cr.Id AND clv.SubmittedToSupervisor = 1
                        WHERE (
                            (@extension = 'Unknown' AND cr.UserPhoneId IS NULL)
                            OR up.PhoneNumber = @extension
                            OR cr.ext_no = @extension
                        )
                        AND cr.call_month = @month
                        AND cr.call_year = @year
                        AND cr.ext_resp_index = @assignedFrom
                        AND clv.Id IS NULL";

                    var eligibleCallIds = await _context.Database
                        .SqlQueryRaw<int>(getEligibleCallsSql,
                            new Microsoft.Data.SqlClient.SqlParameter("@extension", extension),
                            new Microsoft.Data.SqlClient.SqlParameter("@month", month),
                            new Microsoft.Data.SqlClient.SqlParameter("@year", year),
                            new Microsoft.Data.SqlClient.SqlParameter("@assignedFrom", assignedFrom))
                        .ToListAsync();

                    if (!eligibleCallIds.Any())
                    {
                        result.SkippedCount = 0;
                        return;
                    }

                    // Step 2: Bulk insert payment assignments
                    var insertAssignmentsSql = @"
                        INSERT INTO CallLogPaymentAssignments
                            (CallRecordId, AssignedFrom, AssignedTo, AssignmentReason, AssignedDate, AssignmentStatus, CreatedDate, NotificationSent)
                        SELECT
                            cr.Id,
                            @assignedFrom,
                            @assignedTo,
                            @reason,
                            @now,
                            'Pending',
                            @now,
                            0
                        FROM CallRecords cr
                        LEFT JOIN UserPhones up ON cr.UserPhoneId = up.Id
                        LEFT JOIN CallLogVerifications clv ON clv.CallRecordId = cr.Id AND clv.SubmittedToSupervisor = 1
                        WHERE (
                            (@extension = 'Unknown' AND cr.UserPhoneId IS NULL)
                            OR up.PhoneNumber = @extension
                            OR cr.ext_no = @extension
                        )
                        AND cr.call_month = @month
                        AND cr.call_year = @year
                        AND cr.ext_resp_index = @assignedFrom
                        AND clv.Id IS NULL";

                    assignmentsInserted = await _context.Database.ExecuteSqlRawAsync(
                        insertAssignmentsSql,
                        new Microsoft.Data.SqlClient.SqlParameter("@extension", extension),
                        new Microsoft.Data.SqlClient.SqlParameter("@month", month),
                        new Microsoft.Data.SqlClient.SqlParameter("@year", year),
                        new Microsoft.Data.SqlClient.SqlParameter("@assignedFrom", assignedFrom),
                        new Microsoft.Data.SqlClient.SqlParameter("@assignedTo", assignedTo),
                        new Microsoft.Data.SqlClient.SqlParameter("@reason", reason),
                        new Microsoft.Data.SqlClient.SqlParameter("@now", now));

                    // Step 3: Update CallRecords with assignment info
                    var updateCallRecordsSql = @"
                        UPDATE cr
                        SET cr.call_pay_index = @assignedTo,
                            cr.assignment_status = 'Pending',
                            cr.payment_assignment_id = pa.Id
                        FROM CallRecords cr
                        LEFT JOIN UserPhones up ON cr.UserPhoneId = up.Id
                        LEFT JOIN CallLogVerifications clv ON clv.CallRecordId = cr.Id AND clv.SubmittedToSupervisor = 1
                        INNER JOIN CallLogPaymentAssignments pa ON pa.CallRecordId = cr.Id
                            AND pa.AssignedFrom = @assignedFrom
                            AND pa.AssignedTo = @assignedTo
                            AND pa.AssignedDate = @now
                        WHERE (
                            (@extension = 'Unknown' AND cr.UserPhoneId IS NULL)
                            OR up.PhoneNumber = @extension
                            OR cr.ext_no = @extension
                        )
                        AND cr.call_month = @month
                        AND cr.call_year = @year
                        AND cr.ext_resp_index = @assignedFrom
                        AND clv.Id IS NULL";

                    await _context.Database.ExecuteSqlRawAsync(
                        updateCallRecordsSql,
                        new Microsoft.Data.SqlClient.SqlParameter("@extension", extension),
                        new Microsoft.Data.SqlClient.SqlParameter("@month", month),
                        new Microsoft.Data.SqlClient.SqlParameter("@year", year),
                        new Microsoft.Data.SqlClient.SqlParameter("@assignedFrom", assignedFrom),
                        new Microsoft.Data.SqlClient.SqlParameter("@assignedTo", assignedTo),
                        new Microsoft.Data.SqlClient.SqlParameter("@now", now));

                    await transaction.CommitAsync();
                });

                result.ReassignedCount = assignmentsInserted;
                _logger.LogInformation(
                    "Bulk reassigned {Count} calls from extension {Extension} ({Month}/{Year}) from {From} to {To}",
                    assignmentsInserted, extension, month, year, assignedFrom, assignedTo);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk reassignment for extension {Extension}", extension);
                result.Errors.Add(ex.Message);
                return result;
            }
        }

        /// <summary>
        /// ULTRA-FAST bulk reassignment using raw SQL for a specific dialed number.
        /// </summary>
        public async Task<BulkReassignmentResult> BulkReassignByDialedNumberRawAsync(
            string extension,
            int month,
            int year,
            string dialedNumber,
            string assignedFrom,
            string assignedTo,
            string reason)
        {
            var result = new BulkReassignmentResult();
            var now = DateTime.UtcNow;

            try
            {
                // Validate assigned-to user exists
                var assignedToUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.IndexNumber == assignedTo);
                if (assignedToUser == null)
                    throw new ArgumentException($"User with index number {assignedTo} not found");

                var strategy = _context.Database.CreateExecutionStrategy();
                var assignmentsInserted = 0;

                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    // Step 1: Bulk insert payment assignments for the specific dialed number
                    var insertAssignmentsSql = @"
                        INSERT INTO CallLogPaymentAssignments
                            (CallRecordId, AssignedFrom, AssignedTo, AssignmentReason, AssignedDate, AssignmentStatus, CreatedDate, NotificationSent)
                        SELECT
                            cr.Id,
                            @assignedFrom,
                            @assignedTo,
                            @reason,
                            @now,
                            'Pending',
                            @now,
                            0
                        FROM CallRecords cr
                        LEFT JOIN UserPhones up ON cr.UserPhoneId = up.Id
                        LEFT JOIN CallLogVerifications clv ON clv.CallRecordId = cr.Id AND clv.SubmittedToSupervisor = 1
                        WHERE (
                            (@extension = 'Unknown' AND cr.UserPhoneId IS NULL)
                            OR up.PhoneNumber = @extension
                            OR cr.ext_no = @extension
                        )
                        AND cr.call_number = @dialedNumber
                        AND cr.call_month = @month
                        AND cr.call_year = @year
                        AND cr.ext_resp_index = @assignedFrom
                        AND clv.Id IS NULL";

                    assignmentsInserted = await _context.Database.ExecuteSqlRawAsync(
                        insertAssignmentsSql,
                        new Microsoft.Data.SqlClient.SqlParameter("@extension", extension),
                        new Microsoft.Data.SqlClient.SqlParameter("@dialedNumber", dialedNumber),
                        new Microsoft.Data.SqlClient.SqlParameter("@month", month),
                        new Microsoft.Data.SqlClient.SqlParameter("@year", year),
                        new Microsoft.Data.SqlClient.SqlParameter("@assignedFrom", assignedFrom),
                        new Microsoft.Data.SqlClient.SqlParameter("@assignedTo", assignedTo),
                        new Microsoft.Data.SqlClient.SqlParameter("@reason", reason),
                        new Microsoft.Data.SqlClient.SqlParameter("@now", now));

                    // Step 2: Update CallRecords with assignment info
                    var updateCallRecordsSql = @"
                        UPDATE cr
                        SET cr.call_pay_index = @assignedTo,
                            cr.assignment_status = 'Pending',
                            cr.payment_assignment_id = pa.Id
                        FROM CallRecords cr
                        LEFT JOIN UserPhones up ON cr.UserPhoneId = up.Id
                        LEFT JOIN CallLogVerifications clv ON clv.CallRecordId = cr.Id AND clv.SubmittedToSupervisor = 1
                        INNER JOIN CallLogPaymentAssignments pa ON pa.CallRecordId = cr.Id
                            AND pa.AssignedFrom = @assignedFrom
                            AND pa.AssignedTo = @assignedTo
                            AND pa.AssignedDate = @now
                        WHERE (
                            (@extension = 'Unknown' AND cr.UserPhoneId IS NULL)
                            OR up.PhoneNumber = @extension
                            OR cr.ext_no = @extension
                        )
                        AND cr.call_number = @dialedNumber
                        AND cr.call_month = @month
                        AND cr.call_year = @year
                        AND cr.ext_resp_index = @assignedFrom
                        AND clv.Id IS NULL";

                    await _context.Database.ExecuteSqlRawAsync(
                        updateCallRecordsSql,
                        new Microsoft.Data.SqlClient.SqlParameter("@extension", extension),
                        new Microsoft.Data.SqlClient.SqlParameter("@dialedNumber", dialedNumber),
                        new Microsoft.Data.SqlClient.SqlParameter("@month", month),
                        new Microsoft.Data.SqlClient.SqlParameter("@year", year),
                        new Microsoft.Data.SqlClient.SqlParameter("@assignedFrom", assignedFrom),
                        new Microsoft.Data.SqlClient.SqlParameter("@assignedTo", assignedTo),
                        new Microsoft.Data.SqlClient.SqlParameter("@now", now));

                    await transaction.CommitAsync();
                });

                result.ReassignedCount = assignmentsInserted;
                _logger.LogInformation(
                    "Bulk reassigned {Count} calls to dialed number {DialedNumber} from {From} to {To}",
                    assignmentsInserted, dialedNumber, assignedFrom, assignedTo);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk reassignment for dialed number {DialedNumber}", dialedNumber);
                result.Errors.Add(ex.Message);
                return result;
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

        /// <summary>
        /// ULTRA-FAST bulk accept all pending assignments for a user using raw SQL.
        /// Supports filtering by extension, month/year, and dialed number.
        /// </summary>
        public async Task<BulkAssignmentResult> BulkAcceptAssignmentsAsync(
            string indexNumber,
            string? assignedFrom = null,
            string? extension = null,
            int? month = null,
            int? year = null,
            string? dialedNumber = null)
        {
            var result = new BulkAssignmentResult();
            var now = DateTime.UtcNow;

            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                var assignmentsUpdated = 0;

                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    // Build additional filter clauses
                    var additionalFilters = new List<string>();
                    var parameters = new List<Microsoft.Data.SqlClient.SqlParameter>
                    {
                        new Microsoft.Data.SqlClient.SqlParameter("@indexNumber", indexNumber),
                        new Microsoft.Data.SqlClient.SqlParameter("@now", now)
                    };

                    if (!string.IsNullOrEmpty(assignedFrom))
                    {
                        additionalFilters.Add("pa.AssignedFrom = @assignedFrom");
                        parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@assignedFrom", assignedFrom));
                    }

                    // For extension/month/year/dialedNumber filtering, we need to join with CallRecords and UserPhones
                    var needsCallRecordJoin = !string.IsNullOrEmpty(extension) || month.HasValue || year.HasValue || !string.IsNullOrEmpty(dialedNumber);
                    var callRecordJoin = needsCallRecordJoin ? "INNER JOIN CallRecords cr ON pa.CallRecordId = cr.Id" : "";
                    var userPhoneJoin = !string.IsNullOrEmpty(extension) ? "INNER JOIN UserPhones up ON cr.UserPhoneId = up.Id" : "";

                    if (!string.IsNullOrEmpty(extension))
                    {
                        additionalFilters.Add("up.PhoneNumber = @extension");
                        parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@extension", extension));
                    }
                    if (month.HasValue)
                    {
                        additionalFilters.Add("cr.call_month = @month");
                        parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@month", month.Value));
                    }
                    if (year.HasValue)
                    {
                        additionalFilters.Add("cr.call_year = @year");
                        parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@year", year.Value));
                    }
                    if (!string.IsNullOrEmpty(dialedNumber))
                    {
                        var actualDialedNumber = dialedNumber == "Subscription" ? "" : dialedNumber;
                        additionalFilters.Add("ISNULL(cr.call_number, '') = @dialedNumber");
                        parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@dialedNumber", actualDialedNumber));
                    }

                    var additionalFilterClause = additionalFilters.Count > 0 ? "AND " + string.Join(" AND ", additionalFilters) : "";

                    // Step 1: Update CallLogPaymentAssignments to Accepted
                    var updateAssignmentsSql = $@"
                        UPDATE pa
                        SET pa.AssignmentStatus = 'Accepted',
                            pa.AcceptedDate = @now,
                            pa.ModifiedDate = @now
                        FROM CallLogPaymentAssignments pa
                        {callRecordJoin}
                        {userPhoneJoin}
                        WHERE pa.AssignedTo = @indexNumber
                          AND pa.AssignmentStatus = 'Pending'
                          {additionalFilterClause}";

                    assignmentsUpdated = await _context.Database.ExecuteSqlRawAsync(
                        updateAssignmentsSql, parameters.ToArray());

                    // Step 2: Update CallRecords assignment status (for all just-accepted assignments)
                    var updateCallRecordsSql = @"
                        UPDATE cr
                        SET cr.assignment_status = 'Accepted'
                        FROM CallRecords cr
                        INNER JOIN CallLogPaymentAssignments pa ON cr.payment_assignment_id = pa.Id
                        WHERE pa.AssignedTo = @indexNumber2
                          AND pa.AssignmentStatus = 'Accepted'
                          AND pa.AcceptedDate = @now2";

                    var parameters2 = new List<Microsoft.Data.SqlClient.SqlParameter>
                    {
                        new Microsoft.Data.SqlClient.SqlParameter("@indexNumber2", indexNumber),
                        new Microsoft.Data.SqlClient.SqlParameter("@now2", now)
                    };

                    await _context.Database.ExecuteSqlRawAsync(updateCallRecordsSql, parameters2.ToArray());

                    await transaction.CommitAsync();
                });

                result.ProcessedCount = assignmentsUpdated;
                _logger.LogInformation(
                    "Bulk accepted {Count} assignments for user {IndexNumber} (extension={Extension}, month={Month}, year={Year})",
                    assignmentsUpdated, indexNumber, extension, month, year);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk accept for user {IndexNumber}", indexNumber);
                result.Errors.Add(ex.Message);
                return result;
            }
        }

        /// <summary>
        /// ULTRA-FAST bulk reject all pending assignments for a user using raw SQL.
        /// Supports filtering by extension, month/year, and dialed number.
        /// </summary>
        public async Task<BulkAssignmentResult> BulkRejectAssignmentsAsync(
            string indexNumber,
            string reason,
            string? assignedFrom = null,
            string? extension = null,
            int? month = null,
            int? year = null,
            string? dialedNumber = null)
        {
            var result = new BulkAssignmentResult();
            var now = DateTime.UtcNow;

            try
            {
                var strategy = _context.Database.CreateExecutionStrategy();
                var assignmentsUpdated = 0;

                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    // Build additional filter clauses
                    var additionalFilters = new List<string>();
                    var parameters = new List<Microsoft.Data.SqlClient.SqlParameter>
                    {
                        new Microsoft.Data.SqlClient.SqlParameter("@indexNumber", indexNumber),
                        new Microsoft.Data.SqlClient.SqlParameter("@now", now),
                        new Microsoft.Data.SqlClient.SqlParameter("@reason", reason)
                    };

                    if (!string.IsNullOrEmpty(assignedFrom))
                    {
                        additionalFilters.Add("pa.AssignedFrom = @assignedFrom");
                        parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@assignedFrom", assignedFrom));
                    }

                    // For extension/month/year/dialedNumber filtering, we need additional joins
                    var needsUserPhoneJoin = !string.IsNullOrEmpty(extension);
                    var userPhoneJoin = needsUserPhoneJoin ? "INNER JOIN UserPhones up ON cr.UserPhoneId = up.Id" : "";

                    if (!string.IsNullOrEmpty(extension))
                    {
                        additionalFilters.Add("up.PhoneNumber = @extension");
                        parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@extension", extension));
                    }
                    if (month.HasValue)
                    {
                        additionalFilters.Add("cr.call_month = @month");
                        parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@month", month.Value));
                    }
                    if (year.HasValue)
                    {
                        additionalFilters.Add("cr.call_year = @year");
                        parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@year", year.Value));
                    }
                    if (!string.IsNullOrEmpty(dialedNumber))
                    {
                        var actualDialedNumber = dialedNumber == "Subscription" ? "" : dialedNumber;
                        additionalFilters.Add("ISNULL(cr.call_number, '') = @dialedNumber");
                        parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@dialedNumber", actualDialedNumber));
                    }

                    var additionalFilterClause = additionalFilters.Count > 0 ? "AND " + string.Join(" AND ", additionalFilters) : "";

                    // Step 1: Update CallRecords - revert payment back to original owner
                    var updateCallRecordsSql = $@"
                        UPDATE cr
                        SET cr.call_pay_index = pa.AssignedFrom,
                            cr.payment_assignment_id = NULL,
                            cr.assignment_status = 'None'
                        FROM CallRecords cr
                        INNER JOIN CallLogPaymentAssignments pa ON cr.payment_assignment_id = pa.Id
                        {userPhoneJoin}
                        WHERE pa.AssignedTo = @indexNumber
                          AND pa.AssignmentStatus = 'Pending'
                          {additionalFilterClause}";

                    await _context.Database.ExecuteSqlRawAsync(updateCallRecordsSql, parameters.ToArray());

                    // Step 2: Update CallLogPaymentAssignments to Rejected
                    // Need to use a subquery since the call records have already been updated
                    var updateAssignmentsSql = $@"
                        UPDATE pa
                        SET pa.AssignmentStatus = 'Rejected',
                            pa.RejectionReason = @reason2,
                            pa.ModifiedDate = @now2
                        FROM CallLogPaymentAssignments pa
                        INNER JOIN CallRecords cr ON pa.CallRecordId = cr.Id
                        {userPhoneJoin.Replace("cr.", "cr2.").Replace("cr2.user_phone_id", "cr.UserPhoneId")}
                        WHERE pa.AssignedTo = @indexNumber2
                          AND pa.AssignmentStatus = 'Pending'
                          {additionalFilterClause.Replace("@", "@2_").Replace("cr.", "cr.").Replace("up.", "up.")}";

                    // Build parameters for second query with unique names
                    var parameters2 = new List<Microsoft.Data.SqlClient.SqlParameter>
                    {
                        new Microsoft.Data.SqlClient.SqlParameter("@indexNumber2", indexNumber),
                        new Microsoft.Data.SqlClient.SqlParameter("@now2", now),
                        new Microsoft.Data.SqlClient.SqlParameter("@reason2", reason)
                    };
                    if (!string.IsNullOrEmpty(assignedFrom))
                        parameters2.Add(new Microsoft.Data.SqlClient.SqlParameter("@2_assignedFrom", assignedFrom));
                    if (!string.IsNullOrEmpty(extension))
                        parameters2.Add(new Microsoft.Data.SqlClient.SqlParameter("@2_extension", extension));
                    if (month.HasValue)
                        parameters2.Add(new Microsoft.Data.SqlClient.SqlParameter("@2_month", month.Value));
                    if (year.HasValue)
                        parameters2.Add(new Microsoft.Data.SqlClient.SqlParameter("@2_year", year.Value));
                    if (!string.IsNullOrEmpty(dialedNumber))
                    {
                        var actualDialedNumber = dialedNumber == "Subscription" ? "" : dialedNumber;
                        parameters2.Add(new Microsoft.Data.SqlClient.SqlParameter("@2_dialedNumber", actualDialedNumber));
                    }

                    assignmentsUpdated = await _context.Database.ExecuteSqlRawAsync(
                        updateAssignmentsSql, parameters2.ToArray());

                    await transaction.CommitAsync();
                });

                result.ProcessedCount = assignmentsUpdated;
                _logger.LogInformation(
                    "Bulk rejected {Count} assignments for user {IndexNumber} (extension={Extension}, month={Month}, year={Year})",
                    assignmentsUpdated, indexNumber, extension, month, year);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk reject for user {IndexNumber}", indexNumber);
                result.Errors.Add(ex.Message);
                return result;
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

                // Check if any verifications are already submitted and still pending approval
                var alreadySubmitted = verifications
                    .Where(v => v.SubmittedToSupervisor &&
                               (v.ApprovalStatus == "Pending" ||
                                v.ApprovalStatus == "Approved" ||
                                v.ApprovalStatus == "PartiallyApproved"))
                    .ToList();

                if (alreadySubmitted.Any())
                {
                    var submittedIds = string.Join(", ", alreadySubmitted.Select(v => v.Id));
                    throw new InvalidOperationException(
                        $"The following verifications have already been submitted and cannot be resubmitted: {submittedIds}. " +
                        "Only verifications that have been rejected, reverted, or modified can be resubmitted.");
                }

                // Get supervisor from user record
                var user = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.IndexNumber == indexNumber);

                if (user == null || string.IsNullOrEmpty(user.SupervisorEmail))
                    throw new InvalidOperationException("Supervisor not found for user");

                // Look up supervisor by email to get their index number
                var supervisorEbillUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.Email == user.SupervisorEmail);

                // Store both supervisor email and index number for flexible matching
                var supervisorEmail = user.SupervisorEmail;
                var supervisorIndexNumber = supervisorEbillUser?.IndexNumber;

                foreach (var verification in verifications)
                {
                    verification.SubmittedToSupervisor = true;
                    verification.SubmittedDate = DateTime.UtcNow;
                    verification.SupervisorEmail = supervisorEmail;
                    verification.SupervisorIndexNumber = supervisorIndexNumber;
                    verification.ModifiedDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Send notifications
                var totalAmount = verifications.Sum(v => v.ActualAmount);
                var callCount = verifications.Count;

                // Get user's application user ID
                var requesterAppUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == user.Email);

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
                // Get supervisor's email - supervisorIndexNumber could be either an index number or email
                var supervisorEmail = supervisorIndexNumber.Contains("@")
                    ? supervisorIndexNumber
                    : await _context.EbillUsers
                        .Where(u => u.IndexNumber == supervisorIndexNumber)
                        .Select(u => u.Email)
                        .FirstOrDefaultAsync();

                // Match against SupervisorEmail field
                var verification = await _context.CallLogVerifications
                    .Include(v => v.CallRecord)
                    .FirstOrDefaultAsync(v => v.Id == verificationId && v.SupervisorEmail == supervisorEmail);

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
                // Get supervisor's email - supervisorIndexNumber could be either an index number or email
                var supervisorEmail = supervisorIndexNumber.Contains("@")
                    ? supervisorIndexNumber
                    : await _context.EbillUsers
                        .Where(u => u.IndexNumber == supervisorIndexNumber)
                        .Select(u => u.Email)
                        .FirstOrDefaultAsync();

                // Match against SupervisorEmail field
                var verification = await _context.CallLogVerifications
                    .Include(v => v.CallRecord)
                    .FirstOrDefaultAsync(v => v.Id == verificationId && v.SupervisorEmail == supervisorEmail);

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
                // Get supervisor's email - supervisorIndexNumber could be either an index number or email
                var supervisorEmail = supervisorIndexNumber.Contains("@")
                    ? supervisorIndexNumber
                    : await _context.EbillUsers
                        .Where(u => u.IndexNumber == supervisorIndexNumber)
                        .Select(u => u.Email)
                        .FirstOrDefaultAsync();

                // Match against SupervisorEmail field
                var verification = await _context.CallLogVerifications
                    .Include(v => v.CallRecord)
                    .FirstOrDefaultAsync(v => v.Id == verificationId && v.SupervisorEmail == supervisorEmail);

                if (verification == null)
                    return false;

                // Load recovery configuration for revert limits and deadline settings
                var config = await _context.RecoveryConfigurations
                    .FirstOrDefaultAsync(rc => rc.RuleName == "SystemConfiguration");

                var maxReverts = config?.MaxRevertsAllowed ?? 2;
                var revertDays = config?.DefaultRevertDays ?? 3;

                // Check revert limit for the call record
                if (verification.CallRecord != null && verification.CallRecord.RevertCount >= maxReverts)
                {
                    _logger.LogWarning("Cannot revert verification {Id} - maximum reverts ({MaxReverts}) reached for CallRecord {CallRecordId}",
                        verificationId, maxReverts, verification.CallRecord.Id);
                    throw new InvalidOperationException(
                        $"Maximum reverts ({maxReverts}) reached. Staff must verify before the original approval deadline: {verification.CallRecord.ApprovalPeriod:MMM dd, yyyy HH:mm}");
                }

                verification.ApprovalStatus = "Reverted";
                verification.SupervisorApprovalStatus = "Reverted";
                verification.SupervisorApprovedBy = supervisorIndexNumber;
                verification.SupervisorApprovedDate = DateTime.UtcNow;
                verification.SupervisorComments = reason;
                verification.SubmittedToSupervisor = false; // Allow user to resubmit
                verification.ModifiedDate = DateTime.UtcNow;

                // Explicitly mark CallLogVerification as modified to ensure EF tracks all changes
                _context.Entry(verification).State = Microsoft.EntityFrameworkCore.EntityState.Modified;

                // Update call record with revert tracking and deadline reset
                if (verification.CallRecord != null)
                {
                    var callRecord = verification.CallRecord;

                    // Update approval status
                    callRecord.SupervisorApprovalStatus = "Reverted";
                    callRecord.SupervisorApprovedBy = supervisorIndexNumber;
                    callRecord.SupervisorApprovedDate = DateTime.UtcNow;

                    // Track revert
                    callRecord.RevertCount++;
                    callRecord.LastRevertDate = DateTime.UtcNow;
                    callRecord.RevertReason = reason;

                    // Reset verification period for re-verification (staff gets revertDays to fix)
                    callRecord.VerificationPeriod = DateTime.UtcNow.AddDays(revertDays);

                    // IMPORTANT: Keep ApprovalPeriod unchanged - original deadline still applies
                    // This ensures the process doesn't drag on indefinitely

                    // Reset verification status so staff can re-verify
                    callRecord.IsVerified = false;
                    callRecord.VerificationDate = null;

                    _logger.LogInformation(
                        "CallRecord {CallRecordId} reverted (count: {RevertCount}/{MaxReverts}). " +
                        "New verification deadline: {VerificationPeriod}, Original approval deadline unchanged: {ApprovalPeriod}",
                        callRecord.Id, callRecord.RevertCount, maxReverts,
                        callRecord.VerificationPeriod, callRecord.ApprovalPeriod);

                    // Explicitly mark CallRecord as modified to ensure EF tracks the changes
                    _context.Entry(callRecord).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
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
