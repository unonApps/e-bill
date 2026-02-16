using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;

namespace TAB.Web.Pages.Modules.EBillManagement.CallRecords
{
    [Authorize]
    public class SupervisorApprovalsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICallLogVerificationService _verificationService;
        private readonly IClassOfServiceCalculationService _calculationService;
        private readonly IEnhancedEmailService _emailService;
        private readonly ILogger<SupervisorApprovalsModel> _logger;

        public SupervisorApprovalsModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICallLogVerificationService verificationService,
            IClassOfServiceCalculationService calculationService,
            IEnhancedEmailService emailService,
            ILogger<SupervisorApprovalsModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _verificationService = verificationService;
            _calculationService = calculationService;
            _emailService = emailService;
            _logger = logger;
        }

        public List<SupervisorSubmissionGroup> PendingSubmissions { get; set; } = new();
        public string? SupervisorIndexNumber { get; set; }
        public string? SupervisorEmail { get; set; }
        public string? SupervisorName { get; set; }
        public List<StaffOption> StaffWithPendingRequests { get; set; } = new();

        // Filters
        [BindProperty(SupportsGet = true)]
        public string? FilterUser { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FilterStartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FilterEndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterMonth { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? FilterYear { get; set; }

        [BindProperty(SupportsGet = true)]
        public bool FilterOverageOnly { get; set; }

        public class StaffOption
        {
            public string IndexNumber { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        public class SupervisorSubmissionGroup
        {
            public string UserIndexNumber { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public DateTime SubmittedDate { get; set; }
            public int TotalCalls { get; set; }
            public decimal TotalAmount { get; set; }
            public int OverageCount { get; set; }
            public bool HasDocuments { get; set; }
            public List<CallLogVerification> Verifications { get; set; } = new();

            // Phone-level overage justifications
            public List<PhoneOverageJustification> PhoneOverageJustifications { get; set; } = new();
            public bool HasPhoneOverageJustifications => PhoneOverageJustifications.Any();

            // Extension-level overage status
            public Dictionary<string, ExtensionOverageStatus> ExtensionStatuses { get; set; } = new();
        }

        public class ExtensionOverageStatus
        {
            public string PhoneNumber { get; set; } = string.Empty;
            public int UserPhoneId { get; set; }
            public decimal AllowanceLimit { get; set; }
            public decimal CurrentUsage { get; set; }
            public decimal OverageAmount => Math.Max(0, CurrentUsage - AllowanceLimit);
            public bool HasOverage => AllowanceLimit > 0 && CurrentUsage > AllowanceLimit;
            public bool IsUnlimited => AllowanceLimit == 0;
            public string? ClassOfService { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            // Try to get EbillUser record for the supervisor
            var ebillUser = await _context.EbillUsers
                .FirstOrDefaultAsync(u => u.Email == user.Email);

            // SupervisorIndexNumber field in CallLogVerifications may contain either:
            // - The supervisor's IndexNumber (if they exist in EbillUsers)
            // - The supervisor's Email (fallback if no EbillUser record)
            // We store both and check for BOTH to find all submissions
            SupervisorIndexNumber = ebillUser?.IndexNumber;
            SupervisorEmail = user.Email;

            if (ebillUser != null)
            {
                SupervisorName = $"{ebillUser.FirstName} {ebillUser.LastName}";
            }
            else
            {
                SupervisorName = user.FirstName != null && user.LastName != null
                    ? $"{user.FirstName} {user.LastName}"
                    : user.Email;
            }

            // Load list of staff with pending requests for dropdown
            await LoadStaffWithPendingRequestsAsync();

            await LoadPendingSubmissionsAsync();

            return Page();
        }

        private async Task LoadStaffWithPendingRequestsAsync()
        {
            if (string.IsNullOrEmpty(SupervisorEmail))
                return;

            // Get distinct staff members who have pending requests
            var staffIndexNumbers = await _context.CallLogVerifications
                .Where(v => v.SubmittedToSupervisor
                    && v.SupervisorEmail == SupervisorEmail
                    && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"))
                .Select(v => v.VerifiedBy)
                .Distinct()
                .ToListAsync();

            // Get staff details
            var staffUsers = await _context.EbillUsers
                .Where(u => staffIndexNumbers.Contains(u.IndexNumber))
                .ToListAsync();

            var staffOptions = staffUsers.Select(u => new StaffOption
            {
                IndexNumber = u.IndexNumber,
                Name = $"{u.FirstName} {u.LastName}"
            }).ToList();

            // Add any staff not found in EbillUsers (use index number as name)
            foreach (var indexNumber in staffIndexNumbers)
            {
                if (!staffOptions.Any(s => s.IndexNumber == indexNumber))
                {
                    staffOptions.Add(new StaffOption
                    {
                        IndexNumber = indexNumber ?? "",
                        Name = indexNumber ?? "Unknown"
                    });
                }
            }

            StaffWithPendingRequests = staffOptions.OrderBy(s => s.Name).ToList();
        }

        private async Task LoadPendingSubmissionsAsync()
        {
            if (string.IsNullOrEmpty(SupervisorEmail))
                return;

            // Get all verifications submitted to this supervisor that are pending approval
            // Only show records that haven't been acted upon yet (NULL or explicitly "Pending")
            var query = _context.CallLogVerifications
                .Include(v => v.CallRecord)
                    .ThenInclude(c => c.UserPhone)
                        .ThenInclude(up => up.ClassOfService)
                .Include(v => v.Documents)
                .Where(v => v.SubmittedToSupervisor
                    && v.SupervisorEmail == SupervisorEmail
                    && (v.SupervisorApprovalStatus == null ||
                        v.SupervisorApprovalStatus == "" ||
                        v.SupervisorApprovalStatus == "Pending"));

            // Apply filters
            if (!string.IsNullOrEmpty(FilterUser))
            {
                // FilterUser can be either index number or name
                // First check if it matches any index number directly
                var matchingStaff = _context.EbillUsers
                    .Where(u => u.IndexNumber == FilterUser ||
                               (u.FirstName + " " + u.LastName).Contains(FilterUser))
                    .Select(u => u.IndexNumber)
                    .ToList();

                if (matchingStaff.Any())
                {
                    query = query.Where(v => matchingStaff.Contains(v.VerifiedBy));
                }
                else
                {
                    // If no match in EbillUsers, try direct index number match
                    query = query.Where(v => v.VerifiedBy == FilterUser);
                }
            }

            if (FilterStartDate.HasValue)
            {
                query = query.Where(v => v.SubmittedDate >= FilterStartDate.Value);
            }

            if (FilterEndDate.HasValue)
            {
                query = query.Where(v => v.SubmittedDate <= FilterEndDate.Value);
            }

            // Month and Year filters
            if (FilterMonth.HasValue && FilterYear.HasValue)
            {
                query = query.Where(v => v.SubmittedDate.HasValue
                    && v.SubmittedDate.Value.Month == FilterMonth.Value
                    && v.SubmittedDate.Value.Year == FilterYear.Value);
            }
            else if (FilterMonth.HasValue)
            {
                query = query.Where(v => v.SubmittedDate.HasValue
                    && v.SubmittedDate.Value.Month == FilterMonth.Value);
            }
            else if (FilterYear.HasValue)
            {
                query = query.Where(v => v.SubmittedDate.HasValue
                    && v.SubmittedDate.Value.Year == FilterYear.Value);
            }

            if (FilterOverageOnly)
            {
                query = query.Where(v => v.IsOverage);
            }

            var verifications = await query.OrderByDescending(v => v.SubmittedDate).ToListAsync();

            // Group by user and submission date (same day submissions grouped together)
            PendingSubmissions = verifications
                .GroupBy(v => new {
                    v.VerifiedBy,
                    SubmissionDate = v.SubmittedDate.HasValue ? v.SubmittedDate.Value.Date : DateTime.MinValue
                })
                .Select(g => new SupervisorSubmissionGroup
                {
                    UserIndexNumber = g.Key.VerifiedBy ?? "",
                    UserName = GetUserName(g.Key.VerifiedBy ?? ""),
                    SubmittedDate = g.Key.SubmissionDate,
                    TotalCalls = g.Count(),
                    TotalAmount = g.Sum(v => v.ActualAmount),
                    OverageCount = g.Count(v => v.IsOverage),
                    HasDocuments = g.Any(v => v.Documents.Any()),
                    Verifications = g.ToList()
                })
                .OrderByDescending(g => g.SubmittedDate)
                .ToList();

            // Load phone overage justifications for each submission group
            await LoadPhoneOverageJustificationsAsync();

            // Load extension-level overage statuses
            await LoadExtensionStatusesAsync();
        }

        /// <summary>
        /// Loads phone-level overage justifications for pending submissions
        /// </summary>
        private async Task LoadPhoneOverageJustificationsAsync()
        {
            foreach (var submission in PendingSubmissions)
            {
                // Get call records for this submission to find the month/year
                var callRecords = submission.Verifications
                    .Where(v => v.CallRecord != null)
                    .Select(v => v.CallRecord)
                    .ToList();

                if (!callRecords.Any())
                    continue;

                // Get month and year from the first call
                var month = callRecords.First().CallMonth;
                var year = callRecords.First().CallYear;

                // Get all UserPhoneIds from these call records
                var userPhoneIds = callRecords
                    .Where(c => c.UserPhoneId.HasValue)
                    .Select(c => c.UserPhoneId.Value)
                    .Distinct()
                    .ToList();

                if (!userPhoneIds.Any())
                    continue;

                // Load phone overage justifications for these phones and month
                var justifications = await _context.PhoneOverageJustifications
                    .Include(j => j.UserPhone)
                        .ThenInclude(up => up.ClassOfService)
                    .Include(j => j.Documents)
                    .Where(j => userPhoneIds.Contains(j.UserPhoneId)
                        && j.Month == month
                        && j.Year == year
                        && j.ApprovalStatus == "Pending") // Only show pending justifications
                    .ToListAsync();

                // Validate and clean up obsolete justifications before displaying to supervisor
                var validJustifications = new List<PhoneOverageJustification>();
                foreach (var justification in justifications)
                {
                    // Recalculate current usage for this phone
                    var allowanceLimit = await _calculationService.GetPhoneAllowanceLimitAsync(justification.UserPhoneId);
                    var currentUsage = await _calculationService.GetPhoneMonthlyUsageAsync(
                        justification.UserPhoneId,
                        month,
                        year);

                    var hasOverage = allowanceLimit.HasValue &&
                                    allowanceLimit.Value > 0 &&
                                    currentUsage > allowanceLimit.Value;

                    if (!hasOverage)
                    {
                        // No longer an overage - delete the obsolete justification
                        _logger.LogInformation(
                            "Removing obsolete justification {JustificationId} for UserPhoneId {UserPhoneId} for {Month}/{Year}. " +
                            "Current usage ({CurrentUsage}) is now within allowance limit ({Limit}).",
                            justification.Id, justification.UserPhoneId, month, year, currentUsage, allowanceLimit ?? 0);

                        // Delete associated documents first
                        if (justification.Documents?.Any() == true)
                        {
                            foreach (var doc in justification.Documents.ToList())
                            {
                                // Delete physical file
                                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", doc.FilePath.TrimStart('/'));
                                if (System.IO.File.Exists(filePath))
                                {
                                    try
                                    {
                                        System.IO.File.Delete(filePath);
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Failed to delete document file: {FilePath}", filePath);
                                    }
                                }
                                _context.PhoneOverageDocuments.Remove(doc);
                            }
                        }

                        // Delete justification record
                        _context.PhoneOverageJustifications.Remove(justification);
                        _logger.LogInformation("Successfully removed obsolete justification {JustificationId}", justification.Id);
                    }
                    else
                    {
                        // Still valid - update amounts if they've changed
                        var currentOverageAmount = currentUsage - allowanceLimit.Value;
                        if (Math.Abs(justification.TotalUsage - currentUsage) > 0.01m ||
                            Math.Abs(justification.OverageAmount - currentOverageAmount) > 0.01m)
                        {
                            _logger.LogInformation(
                                "Updating justification {JustificationId} amounts. " +
                                "Old: Usage={OldUsage}, Overage={OldOverage}. New: Usage={NewUsage}, Overage={NewOverage}",
                                justification.Id, justification.TotalUsage, justification.OverageAmount,
                                currentUsage, currentOverageAmount);

                            justification.TotalUsage = currentUsage;
                            justification.OverageAmount = currentOverageAmount;
                            justification.AllowanceLimit = allowanceLimit.Value;
                        }

                        validJustifications.Add(justification);
                    }
                }

                // Save all changes
                await _context.SaveChangesAsync();

                submission.PhoneOverageJustifications = validJustifications;
            }
        }

        /// <summary>
        /// Loads extension-level overage status for each phone in pending submissions
        /// </summary>
        private async Task LoadExtensionStatusesAsync()
        {
            foreach (var submission in PendingSubmissions)
            {
                // Get call records for this submission
                var callRecords = submission.Verifications
                    .Where(v => v.CallRecord != null)
                    .Select(v => v.CallRecord!)
                    .ToList();

                if (!callRecords.Any())
                    continue;

                // Get month and year
                var month = callRecords.First().CallMonth;
                var year = callRecords.First().CallYear;

                // Group by extension/phone
                var phoneGroups = callRecords
                    .Where(c => c.UserPhoneId.HasValue && c.UserPhone != null)
                    .GroupBy(c => c.UserPhone!.PhoneNumber)
                    .ToList();

                foreach (var phoneGroup in phoneGroups)
                {
                    var phoneNumber = phoneGroup.Key;
                    var firstCall = phoneGroup.First();
                    var userPhoneId = firstCall.UserPhoneId!.Value;
                    var userPhone = firstCall.UserPhone!;

                    // Get allowance limit and current usage
                    var allowanceLimit = await _calculationService.GetPhoneAllowanceLimitAsync(userPhoneId);
                    var currentUsage = await _calculationService.GetPhoneMonthlyUsageAsync(
                        userPhoneId,
                        month,
                        year);

                    var status = new ExtensionOverageStatus
                    {
                        PhoneNumber = phoneNumber,
                        UserPhoneId = userPhoneId,
                        AllowanceLimit = allowanceLimit ?? 0,
                        CurrentUsage = currentUsage,
                        ClassOfService = userPhone.ClassOfService?.Class
                    };

                    submission.ExtensionStatuses[phoneNumber] = status;
                }
            }
        }

        private string GetUserName(string indexNumber)
        {
            var user = _context.EbillUsers.FirstOrDefault(u => u.IndexNumber == indexNumber);
            return user != null ? $"{user.FirstName} {user.LastName}" : indexNumber;
        }

        public async Task<IActionResult> OnPostApproveSelectedAsync(List<int> verificationIds)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            if (verificationIds == null || !verificationIds.Any())
            {
                StatusMessage = "No verifications selected.";
                StatusMessageClass = "warning";
                return RedirectToPage();
            }

            try
            {
                // Get the supervisor's identifier (IndexNumber if exists, otherwise Email)
                var ebillUser = await _context.EbillUsers.FirstOrDefaultAsync(u => u.Email == user.Email);
                var supervisorIdentifier = ebillUser?.IndexNumber ?? user.Email;

                int successCount = 0;
                int failCount = 0;

                foreach (var verificationId in verificationIds)
                {
                    // Use the service method which properly updates both tables
                    var result = await _verificationService.ApproveVerificationAsync(
                        verificationId,
                        supervisorIdentifier, // Use IndexNumber or Email
                        null, // approvedAmount - null means approve full amount
                        null  // comments
                    );

                    if (result)
                        successCount++;
                    else
                        failCount++;
                }

                if (successCount > 0)
                {
                    // Send approval emails
                    try
                    {
                        foreach (var verificationId in verificationIds)
                        {
                            var verification = await _context.CallLogVerifications
                                .Include(v => v.CallRecord)
                                .FirstOrDefaultAsync(v => v.Id == verificationId);

                            if (verification != null)
                            {
                                var staff = await _context.EbillUsers
                                    .FirstOrDefaultAsync(u => u.IndexNumber == verification.VerifiedBy);

                                if (staff != null)
                                {
                                    await SendApprovalEmailAsync(verification, staff, user.Email ?? "");
                                    _logger.LogInformation("Sent approval email for verification {VerificationId}", verificationId);
                                }
                            }
                        }
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send approval emails");
                        // Don't fail the workflow if email fails
                    }

                    StatusMessage = failCount > 0
                        ? $"Approved {successCount} verification(s). {failCount} failed."
                        : $"Successfully approved {successCount} call verification(s).";
                    StatusMessageClass = failCount > 0 ? "warning" : "success";
                }
                else
                {
                    StatusMessage = "Failed to approve verifications.";
                    StatusMessageClass = "danger";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error approving verifications: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        private async Task SendApprovalEmailAsync(CallLogVerification verification, EbillUser staff, string supervisorEmail)
        {
            var supervisor = await _context.EbillUsers.FirstOrDefaultAsync(u => u.Email == supervisorEmail);
            if (supervisor == null) return;

            var callMonth = verification.CallRecord?.CallMonth ?? DateTime.UtcNow.Month;
            var callYear = verification.CallRecord?.CallYear ?? DateTime.UtcNow.Year;
            var monthName = new DateTime(callYear, callMonth, 1).ToString("MMMM");

            // Determine currency based on SourceSystem
            var sourceSystem = verification.CallRecord?.SourceSystem?.ToUpperInvariant() ?? "";
            var currency = sourceSystem switch
            {
                "PW" or "PRIVATEWIRE" => "USD",
                _ => "KSH"  // Safaricom, Artel, PTNS default to KSH
            };

            // Determine if full or partial approval
            bool isPartialApproval = verification.ApprovedAmount.HasValue &&
                                    verification.ApprovedAmount.Value < verification.ActualAmount;

            if (isPartialApproval)
            {
                // Send partial approval email
                var placeholders = new Dictionary<string, string>
                {
                    { "StaffName", $"{staff.FirstName} {staff.LastName}" },
                    { "Month", monthName },
                    { "Year", callYear.ToString() },
                    { "TotalAmount", verification.ActualAmount.ToString("N2") },
                    { "ApprovedAmount", verification.ApprovedAmount?.ToString("N2") ?? "0.00" },
                    { "StaffPayableAmount", (verification.ActualAmount - (verification.ApprovedAmount ?? 0)).ToString("N2") },
                    { "SupervisorName", $"{supervisor.FirstName} {supervisor.LastName}" },
                    { "Currency", currency },
                    { "ViewCallLogsLink", $"{Request.Scheme}://{Request.Host}/Modules/EBillManagement/CallRecords/MyCallLogs" }
                };

                await _emailService.SendTemplatedEmailAsync(
                    to: staff.Email ?? "",
                    templateCode: "CALL_LOG_PARTIALLY_APPROVED",
                    data: placeholders
                );
            }
            else
            {
                // Send full approval email
                var placeholders = new Dictionary<string, string>
                {
                    { "StaffName", $"{staff.FirstName} {staff.LastName}" },
                    { "Month", monthName },
                    { "Year", callYear.ToString() },
                    { "TotalCalls", "1" }, // Single verification
                    { "ApprovedAmount", verification.ApprovedAmount?.ToString("N2") ?? verification.ActualAmount.ToString("N2") },
                    { "SupervisorName", $"{supervisor.FirstName} {supervisor.LastName}" },
                    { "ApprovedDate", verification.SupervisorApprovedDate?.ToString("MMMM dd, yyyy") ?? DateTime.UtcNow.ToString("MMMM dd, yyyy") },
                    { "Currency", currency },
                    { "ViewCallLogsLink", $"{Request.Scheme}://{Request.Host}/Modules/EBillManagement/CallRecords/MyCallLogs" }
                };

                await _emailService.SendTemplatedEmailAsync(
                    to: staff.Email ?? "",
                    templateCode: "CALL_LOG_APPROVED",
                    data: placeholders
                );
            }
        }

        public async Task<IActionResult> OnPostRejectAsync(List<int> verificationIds, string rejectionReason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            if (verificationIds == null || !verificationIds.Any())
            {
                StatusMessage = "No verifications selected.";
                StatusMessageClass = "warning";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                StatusMessage = "Rejection reason is required.";
                StatusMessageClass = "warning";
                return RedirectToPage();
            }

            try
            {
                // Get the supervisor's identifier (IndexNumber if exists, otherwise Email)
                var ebillUser = await _context.EbillUsers.FirstOrDefaultAsync(u => u.Email == user.Email);
                var supervisorIdentifier = ebillUser?.IndexNumber ?? user.Email;

                int successCount = 0;
                int failCount = 0;

                foreach (var verificationId in verificationIds)
                {
                    var result = await _verificationService.RejectVerificationAsync(
                        verificationId,
                        supervisorIdentifier, // Use IndexNumber or Email
                        rejectionReason
                    );

                    if (result)
                        successCount++;
                    else
                        failCount++;
                }

                if (successCount > 0)
                {
                    // Send rejection emails
                    try
                    {
                        foreach (var verificationId in verificationIds)
                        {
                            var verification = await _context.CallLogVerifications
                                .Include(v => v.CallRecord)
                                .FirstOrDefaultAsync(v => v.Id == verificationId);

                            if (verification != null)
                            {
                                var staff = await _context.EbillUsers
                                    .FirstOrDefaultAsync(u => u.IndexNumber == verification.VerifiedBy);

                                if (staff != null)
                                {
                                    var supervisor = await _context.EbillUsers
                                        .FirstOrDefaultAsync(u => u.Email == user.Email);

                                    var callMonth = verification.CallRecord?.CallMonth ?? DateTime.UtcNow.Month;
                                    var callYear = verification.CallRecord?.CallYear ?? DateTime.UtcNow.Year;
                                    var monthName = new DateTime(callYear, callMonth, 1).ToString("MMMM");

                                    var rejSourceSystem = verification.CallRecord?.SourceSystem?.ToUpperInvariant() ?? "";
                                    var rejCurrency = rejSourceSystem switch
                                    {
                                        "PW" or "PRIVATEWIRE" => "USD",
                                        _ => "KSH"
                                    };

                                    var placeholders = new Dictionary<string, string>
                                    {
                                        { "StaffName", $"{staff.FirstName} {staff.LastName}" },
                                        { "Month", monthName },
                                        { "Year", callYear.ToString() },
                                        { "Currency", rejCurrency },
                                        { "TotalAmount", verification.ActualAmount.ToString("N2") },
                                        { "StaffPayableAmount", verification.ActualAmount.ToString("N2") },
                                        { "SupervisorName", $"{supervisor?.FirstName} {supervisor?.LastName}" ?? user.Email },
                                        { "RejectionReason", rejectionReason },
                                        { "RejectionDate", verification.SupervisorApprovedDate?.ToString("MMMM dd, yyyy") ?? DateTime.UtcNow.ToString("MMMM dd, yyyy") },
                                        { "ViewCallLogsLink", $"{Request.Scheme}://{Request.Host}/Modules/EBillManagement/CallRecords/MyCallLogs" }
                                    };

                                    await _emailService.SendTemplatedEmailAsync(
                                        to: staff.Email ?? "",
                                        templateCode: "CALL_LOG_REJECTED",
                                        data: placeholders
                                    );
                                }
                            }
                        }
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send rejection emails");
                    }
                }

                StatusMessage = $"Successfully rejected {successCount} verification(s).";
                if (failCount > 0)
                {
                    StatusMessage += $" Failed to reject {failCount} verification(s).";
                }
                StatusMessageClass = successCount > 0 ? "success" : "danger";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting verifications");
                StatusMessage = $"An error occurred: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRevertAsync(List<int> verificationIds, string revertReason)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            if (verificationIds == null || !verificationIds.Any())
            {
                StatusMessage = "No verifications selected.";
                StatusMessageClass = "warning";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(revertReason))
            {
                StatusMessage = "Revert reason is required.";
                StatusMessageClass = "warning";
                return RedirectToPage();
            }

            try
            {
                // Get the supervisor's identifier (IndexNumber if exists, otherwise Email)
                var ebillUser = await _context.EbillUsers.FirstOrDefaultAsync(u => u.Email == user.Email);
                var supervisorIdentifier = ebillUser?.IndexNumber ?? user.Email;

                int successCount = 0;
                int failCount = 0;

                foreach (var verificationId in verificationIds)
                {
                    var result = await _verificationService.RevertVerificationAsync(
                        verificationId,
                        supervisorIdentifier, // Use IndexNumber or Email
                        revertReason
                    );

                    if (result)
                        successCount++;
                    else
                        failCount++;
                }

                if (successCount > 0)
                {
                    // Send revert emails
                    try
                    {
                        foreach (var verificationId in verificationIds)
                        {
                            var verification = await _context.CallLogVerifications
                                .Include(v => v.CallRecord)
                                .FirstOrDefaultAsync(v => v.Id == verificationId);

                            if (verification != null)
                            {
                                var staff = await _context.EbillUsers
                                    .FirstOrDefaultAsync(u => u.IndexNumber == verification.VerifiedBy);

                                if (staff != null)
                                {
                                    var supervisor = await _context.EbillUsers
                                        .FirstOrDefaultAsync(u => u.Email == user.Email);

                                    var callMonth = verification.CallRecord?.CallMonth ?? DateTime.UtcNow.Month;
                                    var callYear = verification.CallRecord?.CallYear ?? DateTime.UtcNow.Year;
                                    var monthName = new DateTime(callYear, callMonth, 1).ToString("MMMM");

                                    var placeholders = new Dictionary<string, string>
                                    {
                                        { "StaffName", $"{staff.FirstName} {staff.LastName}" },
                                        { "Month", monthName },
                                        { "Year", callYear.ToString() },
                                        { "TotalCalls", "1" },
                                        { "SupervisorName", $"{supervisor?.FirstName} {supervisor?.LastName}" ?? user.Email },
                                        { "RevertDate", verification.SupervisorApprovedDate?.ToString("MMMM dd, yyyy") ?? DateTime.UtcNow.ToString("MMMM dd, yyyy") },
                                        { "RevertDeadline", verification.RevertDeadline?.ToString("MMMM dd, yyyy") ?? "Not specified" },
                                        { "ViewCallLogsLink", $"{Request.Scheme}://{Request.Host}/Modules/EBillManagement/CallRecords/MyCallLogs" }
                                    };

                                    await _emailService.SendTemplatedEmailAsync(
                                        to: staff.Email ?? "",
                                        templateCode: "CALL_LOG_REVERTED",
                                        data: placeholders
                                    );
                                }
                            }
                        }
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send revert emails");
                    }
                }

                StatusMessage = $"Successfully reverted {successCount} verification(s) back to staff.";
                if (failCount > 0)
                {
                    StatusMessage += $" Failed to revert {failCount} verification(s).";
                }
                StatusMessageClass = successCount > 0 ? "success" : "danger";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reverting verifications");
                StatusMessage = $"An error occurred: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostApprovePartiallyAsync(string PartialApprovalsJson, string SupervisorComments)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            if (string.IsNullOrWhiteSpace(PartialApprovalsJson))
            {
                StatusMessage = "No partial approvals data provided.";
                StatusMessageClass = "warning";
                return RedirectToPage();
            }

            try
            {
                // Deserialize the partial approvals
                var partialApprovals = System.Text.Json.JsonSerializer.Deserialize<List<PartialApprovalData>>(
                    PartialApprovalsJson,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (partialApprovals == null || !partialApprovals.Any())
                {
                    StatusMessage = "No partial approvals data found.";
                    StatusMessageClass = "warning";
                    return RedirectToPage();
                }

                // Get the supervisor's identifier (IndexNumber if exists, otherwise Email)
                var ebillUser = await _context.EbillUsers.FirstOrDefaultAsync(u => u.Email == user.Email);
                var supervisorIdentifier = ebillUser?.IndexNumber ?? user.Email;

                int successCount = 0;
                int failCount = 0;

                foreach (var approval in partialApprovals)
                {
                    // Use the service method with approved amount for partial approval
                    var result = await _verificationService.ApproveVerificationAsync(
                        approval.VerificationId,
                        supervisorIdentifier, // Use IndexNumber or Email
                        approval.ApprovedAmount, // Partial approved amount
                        SupervisorComments
                    );

                    if (result)
                        successCount++;
                    else
                        failCount++;
                }

                if (successCount > 0)
                {
                    // Send approval emails
                    try
                    {
                        foreach (var approval in partialApprovals)
                        {
                            var verification = await _context.CallLogVerifications
                                .Include(v => v.CallRecord)
                                .FirstOrDefaultAsync(v => v.Id == approval.VerificationId);

                            if (verification != null)
                            {
                                var staff = await _context.EbillUsers
                                    .FirstOrDefaultAsync(u => u.IndexNumber == verification.VerifiedBy);

                                if (staff != null)
                                {
                                    await SendApprovalEmailAsync(verification, staff, user.Email ?? "");
                                    _logger.LogInformation("Sent partial approval email for verification {VerificationId}", approval.VerificationId);
                                }
                            }
                        }
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Failed to send partial approval emails");
                        // Don't fail the workflow if email fails
                    }

                    StatusMessage = failCount > 0
                        ? $"Partially approved {successCount} verification(s). {failCount} failed."
                        : $"Successfully partially approved {successCount} verification(s).";
                    StatusMessageClass = failCount > 0 ? "warning" : "success";
                }
                else
                {
                    StatusMessage = "Failed to process partial approvals.";
                    StatusMessageClass = "danger";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing partial approvals");
                StatusMessage = $"Error processing partial approvals: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        // Helper class for deserializing partial approval data
        public class PartialApprovalData
        {
            public int VerificationId { get; set; }
            public decimal ApprovedAmount { get; set; }
            public string Reason { get; set; } = string.Empty;
        }
    }
}
