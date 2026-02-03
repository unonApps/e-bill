using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Models.Enums;
using TAB.Web.Services;

namespace TAB.Web.Pages.Modules.EBillManagement.CallRecords
{
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class SubmitToSupervisorModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICallLogVerificationService _verificationService;
        private readonly IClassOfServiceCalculationService _calculationService;
        private readonly IDocumentManagementService _documentService;
        private readonly IEnhancedEmailService _emailService;
        private readonly ILogger<SubmitToSupervisorModel> _logger;

        public SubmitToSupervisorModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICallLogVerificationService verificationService,
            IClassOfServiceCalculationService calculationService,
            IDocumentManagementService documentService,
            IEnhancedEmailService emailService,
            ILogger<SubmitToSupervisorModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _verificationService = verificationService;
            _calculationService = calculationService;
            _documentService = documentService;
            _emailService = emailService;
            _logger = logger;
        }

        public List<CallRecord> CallRecordsToSubmit { get; set; } = new();
        public List<DialedNumberGroup> GroupedCallRecords { get; set; } = new();
        public string? UserIndexNumber { get; set; }
        public EbillUser? CurrentUser { get; set; }
        public decimal TotalCost { get; set; }
        public decimal OfficialCallsCost { get; set; }
        public decimal PersonalCallsCost { get; set; }
        public int TotalCalls { get; set; }
        public decimal MonthlyAllowance { get; set; }
        public bool HasOverage { get; set; }
        public decimal OverageAmount { get; set; }
        public int CallMonth { get; set; }
        public int CallYear { get; set; }

        // Phone-Level Overage Information
        public List<PhoneOverageInfo> PhoneOverages { get; set; } = new();
        public bool HasAnyPhoneOverage => PhoneOverages.Any(p => p.HasOverage);

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        [TempData]
        public string? StoredCallIds { get; set; }

        /// <summary>
        /// POST handler to receive call IDs (for large selections that exceed URL length limits)
        /// Stores IDs in TempData and redirects to GET
        /// </summary>
        public IActionResult OnPostPrepareSubmission(string? callIdsCsv)
        {
            if (string.IsNullOrEmpty(callIdsCsv))
            {
                StatusMessage = "No calls selected for submission.";
                StatusMessageClass = "warning";
                return RedirectToPage("/Modules/EBillManagement/CallRecords/MyCallLogs");
            }

            // Store call IDs in TempData and redirect to GET
            StoredCallIds = callIdsCsv;
            return RedirectToPage(new { ids = (string?)null }); // Redirect to OnGetAsync
        }

        public async Task<IActionResult> OnGetAsync(string? ids)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            CurrentUser = await _context.EbillUsers
                .FirstOrDefaultAsync(u => u.Email == user.Email);

            if (CurrentUser == null)
            {
                StatusMessage = "Your profile is not linked to an Staff record.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Modules/EBillManagement/CallRecords/MyCallLogs");
            }

            UserIndexNumber = CurrentUser.IndexNumber;

            // Check if supervisor is assigned (SupervisorEmail is required for submission)
            if (string.IsNullOrEmpty(CurrentUser.SupervisorEmail))
            {
                StatusMessage = "No supervisor assigned to your profile. Please contact ICT Service Desk to have a supervisor assigned.";
                StatusMessageClass = "warning";
                return RedirectToPage("/Modules/EBillManagement/CallRecords/MyCallLogs");
            }

            // Check for IDs from URL or TempData (POST redirect)
            var effectiveIds = ids;
            if (string.IsNullOrEmpty(effectiveIds) && !string.IsNullOrEmpty(StoredCallIds))
            {
                effectiveIds = StoredCallIds;
            }

            // Parse call IDs
            List<int> idList = new List<int>();
            if (!string.IsNullOrEmpty(effectiveIds))
            {
                idList = effectiveIds.Split(',').Select(int.Parse).ToList();
            }

            if (!idList.Any())
            {
                StatusMessage = "No calls selected for submission.";
                StatusMessageClass = "warning";
                return RedirectToPage("/Modules/EBillManagement/CallRecords/MyCallLogs");
            }

            // Auto-verify unverified calls as "Official" before submission
            // This allows users to submit without manually verifying each call (defaults to Official)
            var unverifiedCalls = await _context.CallRecords
                .Where(c => idList.Contains(c.Id) &&
                           (c.ResponsibleIndexNumber == UserIndexNumber ||
                            (c.PayingIndexNumber == UserIndexNumber && c.AssignmentStatus == "Accepted")) &&
                           !c.IsVerified &&
                           c.VerificationType != "Personal") // Don't auto-verify Personal calls
                .ToListAsync();

            if (unverifiedCalls.Any())
            {
                foreach (var call in unverifiedCalls)
                {
                    call.IsVerified = true;
                    call.VerificationType = "Official";
                }
                await _context.SaveChangesAsync();
            }

            // Load ALL calls for summary display (now includes auto-verified ones)
            var allCallsQuery = _context.CallRecords
                .Include(c => c.UserPhone)
                    .ThenInclude(up => up.ClassOfService)
                .Where(c => idList.Contains(c.Id) &&
                           (c.ResponsibleIndexNumber == UserIndexNumber ||
                            (c.PayingIndexNumber == UserIndexNumber && c.AssignmentStatus == "Accepted")));

            var allSelectedCalls = await allCallsQuery
                .OrderBy(c => c.CallDate)
                .ToListAsync();

            if (!allSelectedCalls.Any())
            {
                StatusMessage = "No calls found for submission.";
                StatusMessageClass = "warning";
                return RedirectToPage("/Modules/EBillManagement/CallRecords/MyCallLogs");
            }

            // Filter to ONLY Official calls for submission to supervisor
            CallRecordsToSubmit = allSelectedCalls.Where(c => c.VerificationType == "Official").ToList();

            if (!CallRecordsToSubmit.Any())
            {
                StatusMessage = "No official calls selected for submission. All selected calls are marked as Personal.";
                StatusMessageClass = "warning";
                return RedirectToPage("/Modules/EBillManagement/CallRecords/MyCallLogs");
            }

            // Group calls by dialed number
            GroupedCallRecords = CallRecordsToSubmit
                .GroupBy(c => c.CallNumber)
                .Select((g, index) => new DialedNumberGroup
                {
                    CallNumber = g.Key,
                    CallDestination = g.First().CallDestination,
                    GroupId = $"dialed_{index}",
                    Calls = g.OrderBy(c => c.CallDate).ToList()
                })
                .OrderByDescending(g => g.TotalCost)
                .ToList();

            // Get month/year from first call (all should be same month typically)
            CallMonth = CallRecordsToSubmit.First().CallMonth;
            CallYear = CallRecordsToSubmit.First().CallYear;

            // Calculate summary - Total is for Official calls only (being submitted)
            TotalCalls = CallRecordsToSubmit.Count; // Only Official calls count
            OfficialCallsCost = CallRecordsToSubmit.Sum(c => c.CallCostUSD);
            TotalCost = OfficialCallsCost; // Total cost is Official calls cost

            // Calculate Personal calls cost from all selected calls (for display only, not submitted)
            PersonalCallsCost = allSelectedCalls.Where(c => c.VerificationType == "Personal").Sum(c => c.CallCostUSD);

            // Get monthly allowance from Class of Service (kept for backward compatibility)
            var allowanceNullable = await _calculationService.GetAllowanceLimitAsync(UserIndexNumber);
            MonthlyAllowance = allowanceNullable ?? 0; // 0 means unlimited

            // UPDATED: Calculate overage PER PHONE/EXTENSION (not per user)
            await CalculatePhoneLevelOverageAsync(CallRecordsToSubmit);

            // Legacy overage calculation (kept for backward compatibility with old UI)
            if (MonthlyAllowance > 0 && OfficialCallsCost > MonthlyAllowance)
            {
                HasOverage = true;
                OverageAmount = OfficialCallsCost - MonthlyAllowance;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(
            List<int> callRecordIds,
            string? overageJustification,
            IFormFile? overageDocument,
            List<PhoneOverageJustificationDto>? phoneOverageJustifications)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var ebillUser = await _context.EbillUsers
                .FirstOrDefaultAsync(u => u.Email == user.Email);

            if (ebillUser == null)
            {
                StatusMessage = "Your profile is not linked to an Staff record.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Modules/EBillManagement/CallRecords/MyCallLogs");
            }

            if (callRecordIds == null || !callRecordIds.Any())
            {
                StatusMessage = "No calls selected for submission.";
                StatusMessageClass = "warning";
                return RedirectToPage("/Modules/EBillManagement/CallRecords/MyCallLogs");
            }

            try
            {
                // Load call records to check for overage - ONLY Official calls
                var callRecords = await _context.CallRecords
                    .Include(c => c.UserPhone)
                        .ThenInclude(up => up.ClassOfService)
                    .Where(c => callRecordIds.Contains(c.Id))
                    .ToListAsync();

                // Validate that all calls are Official (reject any Personal calls)
                var personalCalls = callRecords.Where(c => c.VerificationType != "Official").ToList();
                if (personalCalls.Any())
                {
                    StatusMessage = "Personal calls cannot be submitted to supervisor. Only official calls can be submitted.";
                    StatusMessageClass = "danger";
                    StoredCallIds = string.Join(",", callRecordIds);
                    return RedirectToPage();
                }

                var officialCallsCost = callRecords.Sum(c => c.CallCostUSD);
                var allowanceNullable = await _calculationService.GetAllowanceLimitAsync(ebillUser.IndexNumber);
                var monthlyAllowance = allowanceNullable ?? 0;

                // Check if overage exists and justification is required
                bool hasOverage = monthlyAllowance > 0 && officialCallsCost > monthlyAllowance;

                if (hasOverage)
                {
                    // Validate justification and document are provided
                    if (string.IsNullOrWhiteSpace(overageJustification))
                    {
                        StatusMessage = "Overage justification is required when official calls exceed your monthly allowance.";
                        StatusMessageClass = "danger";
                        StoredCallIds = string.Join(",", callRecordIds);
                        return RedirectToPage();
                    }

                    if (overageDocument == null)
                    {
                        StatusMessage = "Supporting document is required when official calls exceed your monthly allowance.";
                        StatusMessageClass = "danger";
                        StoredCallIds = string.Join(",", callRecordIds);
                        return RedirectToPage();
                    }
                }

                // Get or create verifications for each call record
                var verificationIds = new List<int>();
                var alreadySubmittedCalls = new List<int>();

                foreach (var callRecordId in callRecordIds)
                {
                    var existingVerification = await _context.CallLogVerifications
                        .FirstOrDefaultAsync(v => v.CallRecordId == callRecordId && v.VerifiedBy == ebillUser.IndexNumber);

                    if (existingVerification != null)
                    {
                        // Check if already submitted and not in a resubmittable state
                        if (existingVerification.SubmittedToSupervisor &&
                            (existingVerification.ApprovalStatus == "Pending" ||
                             existingVerification.ApprovalStatus == "Approved" ||
                             existingVerification.ApprovalStatus == "PartiallyApproved"))
                        {
                            alreadySubmittedCalls.Add(callRecordId);
                            continue; // Skip this one
                        }

                        verificationIds.Add(existingVerification.Id);
                    }
                    else
                    {
                        // Create verification (shouldn't happen if calls are pre-verified, but handle it)
                        var callRecord = callRecords.First(c => c.Id == callRecordId);

                        // Parse verification type from string to enum
                        VerificationType verificationType;
                        if (!Enum.TryParse(callRecord.VerificationType, out verificationType))
                        {
                            verificationType = VerificationType.Official; // Default
                        }

                        var newVerification = new CallLogVerification
                        {
                            CallRecordId = callRecordId,
                            VerifiedBy = ebillUser.IndexNumber,
                            VerifiedDate = DateTime.UtcNow,
                            VerificationType = verificationType,
                            ActualAmount = callRecord.CallCostUSD,
                            JustificationText = overageJustification ?? string.Empty
                        };
                        _context.CallLogVerifications.Add(newVerification);
                        await _context.SaveChangesAsync();
                        verificationIds.Add(newVerification.Id);
                    }
                }

                // Check if any calls were skipped because they're already submitted
                if (alreadySubmittedCalls.Any())
                {
                    if (!verificationIds.Any())
                    {
                        // All calls were already submitted
                        StatusMessage = "All selected calls have already been submitted to supervisor and cannot be resubmitted.";
                        StatusMessageClass = "warning";
                        return RedirectToPage("/Modules/EBillManagement/CallRecords/MyCallLogs");
                    }
                    else
                    {
                        // Some were already submitted, proceed with the rest
                        _logger.LogWarning("Skipped {Count} already-submitted calls. Proceeding with {RemainingCount} calls.",
                            alreadySubmittedCalls.Count, verificationIds.Count);
                    }
                }

                // If overage, upload document and attach to verifications
                if (hasOverage && overageDocument != null)
                {
                    // Upload document for the first verification (or you could create one per verification)
                    var primaryVerificationId = verificationIds.First();
                    var uploadedDoc = await _documentService.UploadDocumentAsync(
                        primaryVerificationId,
                        overageDocument,
                        Models.Enums.DocumentType.OverageJustification,
                        ebillUser.IndexNumber,
                        overageJustification);

                    // Update all verifications with the justification
                    var verificationsToUpdate = await _context.CallLogVerifications
                        .Where(v => verificationIds.Contains(v.Id))
                        .ToListAsync();

                    foreach (var verification in verificationsToUpdate)
                    {
                        verification.JustificationText = overageJustification;
                        verification.OverageJustified = true;
                    }
                    await _context.SaveChangesAsync();
                }

                // NEW: Save phone-level overage justifications
                if (phoneOverageJustifications != null && phoneOverageJustifications.Any())
                {
                    var callMonth = callRecords.First().CallMonth;
                    var callYear = callRecords.First().CallYear;

                    await SavePhoneOverageJustificationsAsync(
                        phoneOverageJustifications,
                        ebillUser.IndexNumber,
                        callMonth,
                        callYear);

                    _logger.LogInformation("Saved {Count} phone overage justifications for user {IndexNumber}",
                        phoneOverageJustifications.Count, ebillUser.IndexNumber);
                }

                // Submit to supervisor
                var submittedCount = await _verificationService.SubmitToSupervisorAsync(
                    verificationIds,
                    ebillUser.IndexNumber);

                // Send email notifications
                try
                {
                    // Get supervisor details (lookup by email)
                    var supervisorUser = await _context.EbillUsers
                        .FirstOrDefaultAsync(u => u.Email == ebillUser.SupervisorEmail);

                    if (supervisorUser != null)
                    {
                        // 1. Send confirmation email to staff
                        await SendSubmittedConfirmationEmailAsync(ebillUser, callRecords, supervisorUser, hasOverage, monthlyAllowance, officialCallsCost, overageJustification);

                        // 2. Send notification email to supervisor
                        await SendSupervisorNotificationEmailAsync(ebillUser, callRecords, supervisorUser, hasOverage, monthlyAllowance, officialCallsCost, overageJustification);

                        _logger.LogInformation("Call log submission emails sent successfully for user {IndexNumber}", ebillUser.IndexNumber);
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send submission emails for user {IndexNumber}", ebillUser.IndexNumber);
                    // Don't fail the workflow if email fails
                }

                StatusMessage = $"Successfully submitted {submittedCount} call verifications to your supervisor for approval.";
                if (alreadySubmittedCalls.Any())
                {
                    StatusMessage += $" Note: {alreadySubmittedCalls.Count} call(s) were skipped because they were already submitted.";
                }
                if (hasOverage)
                {
                    StatusMessage += " Your overage justification and supporting document have been included.";
                }
                if (phoneOverageJustifications != null && phoneOverageJustifications.Any())
                {
                    StatusMessage += $" Submitted {phoneOverageJustifications.Count} extension-level overage justification(s).";
                }
                StatusMessageClass = "success";
                return RedirectToPage("/Modules/EBillManagement/CallRecords/MyCallLogs");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting verifications for user");
                StatusMessage = $"Error submitting verifications: {ex.Message}";
                StatusMessageClass = "danger";
                StoredCallIds = string.Join(",", callRecordIds);
                return RedirectToPage();
            }
        }

        /// <summary>
        /// Saves phone overage justifications to the database
        /// </summary>
        private async Task SavePhoneOverageJustificationsAsync(
            List<PhoneOverageJustificationDto> phoneJustifications,
            string submittedBy,
            int month,
            int year)
        {
            foreach (var dto in phoneJustifications)
            {
                var userPhoneId = dto.UserPhoneId;

                if (userPhoneId <= 0 || string.IsNullOrWhiteSpace(dto.Justification))
                    continue;

                // Check if justification already exists for this phone/month
                var existingJustification = await _context.PhoneOverageJustifications
                    .FirstOrDefaultAsync(j =>
                        j.UserPhoneId == userPhoneId &&
                        j.Month == month &&
                        j.Year == year);

                if (existingJustification != null)
                {
                    _logger.LogWarning("Overage justification already exists for UserPhoneId {UserPhoneId} for {Month}/{Year}. Skipping.",
                        userPhoneId, month, year);
                    continue;
                }

                // Get phone details and calculate overage amounts
                var allowanceLimit = await _calculationService.GetPhoneAllowanceLimitAsync(userPhoneId);
                if (!allowanceLimit.HasValue || allowanceLimit.Value == 0)
                    continue; // No limit, no overage

                var totalUsage = await _calculationService.GetPhoneMonthlyUsageAsync(userPhoneId, month, year);
                var overageAmount = Math.Max(0, totalUsage - allowanceLimit.Value);

                if (overageAmount <= 0)
                    continue; // No overage, skip

                // Create new justification record
                var justification = new PhoneOverageJustification
                {
                    UserPhoneId = userPhoneId,
                    Month = month,
                    Year = year,
                    AllowanceLimit = allowanceLimit.Value,
                    TotalUsage = totalUsage,
                    OverageAmount = overageAmount,
                    JustificationText = dto.Justification,
                    SubmittedBy = submittedBy,
                    SubmittedDate = DateTime.UtcNow,
                    ApprovalStatus = "Pending"
                };

                _context.PhoneOverageJustifications.Add(justification);
                await _context.SaveChangesAsync();

                // Upload document if provided
                if (dto.Document != null)
                {
                    await UploadPhoneOverageDocumentAsync(justification.Id, dto.Document, submittedBy, dto.Justification);
                }

                _logger.LogInformation("Saved phone overage justification for UserPhoneId {UserPhoneId} for {Month}/{Year}",
                    userPhoneId, month, year);
            }
        }

        /// <summary>
        /// Uploads a supporting document for phone overage justification
        /// </summary>
        private async Task UploadPhoneOverageDocumentAsync(
            int justificationId,
            IFormFile document,
            string uploadedBy,
            string description)
        {
            try
            {
                // Create upload directory if it doesn't exist
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "phone-overage-documents");
                Directory.CreateDirectory(uploadPath);

                // Generate unique filename
                var fileExtension = Path.GetExtension(document.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadPath, uniqueFileName);

                // Save file to disk
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await document.CopyToAsync(stream);
                }

                // Create database record
                var phoneOverageDoc = new PhoneOverageDocument
                {
                    PhoneOverageJustificationId = justificationId,
                    FileName = document.FileName,
                    FilePath = $"/uploads/phone-overage-documents/{uniqueFileName}",
                    FileSize = document.Length,
                    ContentType = document.ContentType,
                    Description = description,
                    UploadedBy = uploadedBy,
                    UploadedDate = DateTime.UtcNow
                };

                _context.PhoneOverageDocuments.Add(phoneOverageDoc);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Uploaded phone overage document for JustificationId {JustificationId}: {FileName}",
                    justificationId, document.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload phone overage document for JustificationId {JustificationId}",
                    justificationId);
                throw;
            }
        }

        /// <summary>
        /// Calculates overage for each phone/extension separately
        /// </summary>
        private async Task CalculatePhoneLevelOverageAsync(List<CallRecord> calls)
        {
            PhoneOverages.Clear();

            // Group calls by UserPhoneId
            var callsByPhone = calls
                .Where(c => c.UserPhoneId.HasValue)
                .GroupBy(c => c.UserPhoneId.Value)
                .ToList();

            foreach (var phoneGroup in callsByPhone)
            {
                var userPhoneId = phoneGroup.Key;
                var phoneCalls = phoneGroup.ToList();

                // Get phone details
                var userPhone = phoneCalls.First().UserPhone;
                if (userPhone == null) continue;

                // Get allowance limit for this specific phone
                var allowanceLimit = await _calculationService.GetPhoneAllowanceLimitAsync(userPhoneId);
                if (allowanceLimit == null || allowanceLimit == 0) continue; // Unlimited or no limit

                // Calculate total usage for this phone in this month
                var totalUsage = await _calculationService.GetPhoneMonthlyUsageAsync(
                    userPhoneId,
                    CallMonth,
                    CallYear);

                // Check if overage justification already exists for this phone/month
                var existingJustification = await _context.PhoneOverageJustifications
                    .Include(j => j.Documents)
                    .FirstOrDefaultAsync(j =>
                        j.UserPhoneId == userPhoneId &&
                        j.Month == CallMonth &&
                        j.Year == CallYear);

                // Check if we need to clean up obsolete justification
                if (existingJustification != null)
                {
                    var hasOverage = allowanceLimit.Value > 0 && totalUsage > allowanceLimit.Value;

                    if (!hasOverage)
                    {
                        // No longer an overage - delete the obsolete justification
                        _logger.LogInformation(
                            "Removing obsolete justification for UserPhoneId {UserPhoneId} for {Month}/{Year}. " +
                            "Total usage ({TotalUsage}) is now within allowance limit ({Limit}).",
                            userPhoneId, CallMonth, CallYear, totalUsage, allowanceLimit.Value);

                        // Delete associated documents first
                        if (existingJustification.Documents?.Any() == true)
                        {
                            foreach (var doc in existingJustification.Documents.ToList())
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
                        _context.PhoneOverageJustifications.Remove(existingJustification);
                        await _context.SaveChangesAsync();

                        existingJustification = null;
                        _logger.LogInformation("Successfully removed obsolete justification for UserPhoneId {UserPhoneId}", userPhoneId);
                    }
                    else
                    {
                        // Still an overage - update amounts if they've changed
                        var currentOverageAmount = totalUsage - allowanceLimit.Value;
                        if (Math.Abs(existingJustification.TotalUsage - totalUsage) > 0.01m ||
                            Math.Abs(existingJustification.OverageAmount - currentOverageAmount) > 0.01m)
                        {
                            _logger.LogInformation(
                                "Updating justification amounts for UserPhoneId {UserPhoneId}. " +
                                "Old: Usage={OldUsage}, Overage={OldOverage}. New: Usage={NewUsage}, Overage={NewOverage}",
                                userPhoneId, existingJustification.TotalUsage, existingJustification.OverageAmount,
                                totalUsage, currentOverageAmount);

                            existingJustification.TotalUsage = totalUsage;
                            existingJustification.OverageAmount = currentOverageAmount;
                            existingJustification.AllowanceLimit = allowanceLimit.Value;
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                var phoneOverage = new PhoneOverageInfo
                {
                    UserPhoneId = userPhoneId,
                    PhoneNumber = userPhone.PhoneNumber,
                    PhoneType = userPhone.PhoneType,
                    ClassOfService = userPhone.ClassOfService?.Class,
                    AllowanceLimit = allowanceLimit.Value,
                    TotalUsage = totalUsage,
                    CallCount = phoneCalls.Count,
                    ExistingJustification = existingJustification
                };

                PhoneOverages.Add(phoneOverage);
            }

            // Sort by overage amount (highest first)
            PhoneOverages = PhoneOverages.OrderByDescending(p => p.OverageAmount).ToList();
        }

        private async Task SendSubmittedConfirmationEmailAsync(EbillUser staff, List<CallRecord> callRecords, EbillUser supervisor, bool hasOverage, decimal monthlyAllowance, decimal totalAmount, string? justification)
        {
            var callMonth = callRecords.First().CallMonth;
            var callYear = callRecords.First().CallYear;
            var monthName = new DateTime(callYear, callMonth, 1).ToString("MMMM");

            var overageMessage = hasOverage
                ? $"⚠ Your calls exceed the monthly allowance by USD {(totalAmount - monthlyAllowance):N2}. Justification has been included."
                : "✓ Your calls are within the monthly allowance.";

            var overageBackgroundColor = hasOverage ? "#fff3cd" : "#d4edda";
            var overageBorderColor = hasOverage ? "#ffc107" : "#28a745";
            var overageTextColor = hasOverage ? "#856404" : "#155724";

            var placeholders = new Dictionary<string, string>
            {
                { "StaffName", $"{staff.FirstName} {staff.LastName}" },
                { "IndexNumber", staff.IndexNumber },
                { "Month", monthName },
                { "Year", callYear.ToString() },
                { "TotalCalls", callRecords.Count.ToString() },
                { "TotalAmount", totalAmount.ToString("N2") },
                { "MonthlyAllowance", monthlyAllowance > 0 ? monthlyAllowance.ToString("N2") : "Unlimited" },
                { "SupervisorName", $"{supervisor.FirstName} {supervisor.LastName}" },
                { "OverageMessage", overageMessage },
                { "OverageBackgroundColor", overageBackgroundColor },
                { "OverageBorderColor", overageBorderColor },
                { "OverageTextColor", overageTextColor },
                { "ViewCallLogsLink", $"{Request.Scheme}://{Request.Host}/Modules/EBillManagement/CallRecords/MyCallLogs" }
            };

            await _emailService.SendTemplatedEmailAsync(
                to: staff.Email ?? "",
                templateCode: "CALL_LOG_SUBMITTED_CONFIRMATION",
                data: placeholders
            );
        }

        private async Task SendSupervisorNotificationEmailAsync(EbillUser staff, List<CallRecord> callRecords, EbillUser supervisor, bool hasOverage, decimal monthlyAllowance, decimal totalAmount, string? justification)
        {
            var callMonth = callRecords.First().CallMonth;
            var callYear = callRecords.First().CallYear;
            var monthName = new DateTime(callYear, callMonth, 1).ToString("MMMM");

            var overageMessage = hasOverage
                ? $"⚠ OVERAGE: Calls exceed allowance by USD {(totalAmount - monthlyAllowance):N2}"
                : "✓ Calls are within allowance";

            var overageBackgroundColor = hasOverage ? "#fadbd8" : "#d4edda";
            var overageBorderColor = hasOverage ? "#dc3545" : "#28a745";
            var overageTextColor = hasOverage ? "#721c24" : "#155724";

            var placeholders = new Dictionary<string, string>
            {
                { "SupervisorName", $"{supervisor.FirstName} {supervisor.LastName}" },
                { "StaffName", $"{staff.FirstName} {staff.LastName}" },
                { "IndexNumber", staff.IndexNumber },
                { "Month", monthName },
                { "Year", callYear.ToString() },
                { "TotalCalls", callRecords.Count.ToString() },
                { "TotalAmount", totalAmount.ToString("N2") },
                { "MonthlyAllowance", monthlyAllowance > 0 ? monthlyAllowance.ToString("N2") : "Unlimited" },
                { "OverageMessage", overageMessage },
                { "OverageBackgroundColor", overageBackgroundColor },
                { "OverageBorderColor", overageBorderColor },
                { "OverageTextColor", overageTextColor },
                { "JustificationText", hasOverage && !string.IsNullOrEmpty(justification) ? justification : "No justification required" },
                { "ApprovalLink", $"{Request.Scheme}://{Request.Host}/Modules/EBillManagement/CallRecords/SupervisorApprovals" }
            };

            await _emailService.SendTemplatedEmailAsync(
                to: supervisor.Email ?? "",
                templateCode: "CALL_LOG_SUPERVISOR_NOTIFICATION",
                data: placeholders
            );
        }
    }

    /// <summary>
    /// Holds overage information for a specific phone/extension
    /// </summary>
    public class PhoneOverageInfo
    {
        public int UserPhoneId { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string PhoneType { get; set; } = string.Empty;
        public string? ClassOfService { get; set; }
        public decimal AllowanceLimit { get; set; }
        public decimal TotalUsage { get; set; }
        public decimal OverageAmount => Math.Max(0, TotalUsage - AllowanceLimit);
        public bool HasOverage => AllowanceLimit > 0 && TotalUsage > AllowanceLimit;
        public int CallCount { get; set; }
        public PhoneOverageJustification? ExistingJustification { get; set; }
        public bool HasExistingJustification => ExistingJustification != null;
    }

    /// <summary>
    /// DTO for binding phone overage justification form data
    /// </summary>
    public class PhoneOverageJustificationDto
    {
        public int UserPhoneId { get; set; }
        public string Justification { get; set; } = string.Empty;
        public IFormFile? Document { get; set; }
    }

    /// <summary>
    /// Groups calls by dialed number
    /// </summary>
    public class DialedNumberGroup
    {
        public string CallNumber { get; set; } = string.Empty;
        public string CallDestination { get; set; } = string.Empty;
        public string GroupId { get; set; } = string.Empty;
        public List<CallRecord> Calls { get; set; } = new();
        public int CallCount => Calls.Count;
        public decimal TotalCost => Calls.Sum(c => c.CallCostUSD);
        public long TotalDuration => Calls.Sum(c => (long)c.CallDuration);
        public string GetDurationFormatted()
        {
            var totalMinutes = TotalDuration / 60.0m;
            if (totalMinutes < 1)
                return $"{TotalDuration}s";
            return $"{totalMinutes:N2}m";
        }
    }
}
