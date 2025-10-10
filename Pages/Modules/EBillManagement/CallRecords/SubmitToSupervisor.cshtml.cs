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
    public class SubmitToSupervisorModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICallLogVerificationService _verificationService;
        private readonly IClassOfServiceCalculationService _calculationService;
        private readonly IDocumentManagementService _documentService;

        public SubmitToSupervisorModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICallLogVerificationService verificationService,
            IClassOfServiceCalculationService calculationService,
            IDocumentManagementService documentService)
        {
            _context = context;
            _userManager = userManager;
            _verificationService = verificationService;
            _calculationService = calculationService;
            _documentService = documentService;
        }

        public List<CallRecord> CallRecordsToSubmit { get; set; } = new();
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

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        public async Task<IActionResult> OnGetAsync(string? ids)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            CurrentUser = await _context.EbillUsers
                .FirstOrDefaultAsync(u => u.Email == user.Email);

            if (CurrentUser == null)
            {
                StatusMessage = "Your profile is not linked to an employee record.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Modules/EBillManagement/CallRecords/MyCallLogs");
            }

            UserIndexNumber = CurrentUser.IndexNumber;

            // Check if supervisor is assigned
            if (string.IsNullOrEmpty(CurrentUser.SupervisorIndexNumber))
            {
                StatusMessage = "No supervisor assigned to your profile. Please contact ICT Service Desk.";
                StatusMessageClass = "warning";
                return RedirectToPage("/Modules/EBillManagement/CallRecords/MyCallLogs");
            }

            // Load ALL verified calls for summary display (both Official and Personal)
            var allCallsQuery = _context.CallRecords
                .Include(c => c.UserPhone)
                    .ThenInclude(up => up.ClassOfService)
                .Where(c => (c.ResponsibleIndexNumber == UserIndexNumber ||
                           (c.PayingIndexNumber == UserIndexNumber && c.AssignmentStatus == "Accepted"))
                       && c.IsVerified);

            // If specific IDs provided, filter to those
            if (!string.IsNullOrEmpty(ids))
            {
                var idList = ids.Split(',').Select(int.Parse).ToList();
                allCallsQuery = allCallsQuery.Where(c => idList.Contains(c.Id));
            }

            var allSelectedCalls = await allCallsQuery
                .OrderBy(c => c.CallDate)
                .ToListAsync();

            if (!allSelectedCalls.Any())
            {
                StatusMessage = "No verified calls selected for submission.";
                StatusMessageClass = "warning";
                return RedirectToPage("/Modules/EBillManagement/CallRecords/MyCallLogs");
            }

            // Filter to ONLY Official calls for submission to supervisor
            CallRecordsToSubmit = allSelectedCalls.Where(c => c.VerificationType == "Official").ToList();

            if (!CallRecordsToSubmit.Any())
            {
                StatusMessage = "No official calls selected for submission. Only official calls can be submitted to supervisor.";
                StatusMessageClass = "warning";
                return RedirectToPage("/Modules/EBillManagement/CallRecords/MyCallLogs");
            }

            // Get month/year from first call (all should be same month typically)
            CallMonth = CallRecordsToSubmit.First().CallMonth;
            CallYear = CallRecordsToSubmit.First().CallYear;

            // Calculate summary - Total is for Official calls only (being submitted)
            TotalCalls = CallRecordsToSubmit.Count; // Only Official calls count
            OfficialCallsCost = CallRecordsToSubmit.Sum(c => c.CallCostUSD);
            TotalCost = OfficialCallsCost; // Total cost is Official calls cost

            // Calculate Personal calls cost from all selected calls (for display only, not submitted)
            PersonalCallsCost = allSelectedCalls.Where(c => c.VerificationType == "Personal").Sum(c => c.CallCostUSD);

            // Get monthly allowance from Class of Service
            var allowanceNullable = await _calculationService.GetAllowanceLimitAsync(UserIndexNumber);
            MonthlyAllowance = allowanceNullable ?? 0; // 0 means unlimited

            // Check for overage (only if allowance is set and official calls exceed it)
            if (MonthlyAllowance > 0 && OfficialCallsCost > MonthlyAllowance)
            {
                HasOverage = true;
                OverageAmount = OfficialCallsCost - MonthlyAllowance;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(List<int> callRecordIds, string? overageJustification, IFormFile? overageDocument)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var ebillUser = await _context.EbillUsers
                .FirstOrDefaultAsync(u => u.Email == user.Email);

            if (ebillUser == null)
            {
                StatusMessage = "Your profile is not linked to an employee record.";
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
                    return RedirectToPage(new { ids = string.Join(",", callRecordIds) });
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
                        return RedirectToPage(new { ids = string.Join(",", callRecordIds) });
                    }

                    if (overageDocument == null)
                    {
                        StatusMessage = "Supporting document is required when official calls exceed your monthly allowance.";
                        StatusMessageClass = "danger";
                        return RedirectToPage(new { ids = string.Join(",", callRecordIds) });
                    }
                }

                // Get or create verifications for each call record
                var verificationIds = new List<int>();
                foreach (var callRecordId in callRecordIds)
                {
                    var existingVerification = await _context.CallLogVerifications
                        .FirstOrDefaultAsync(v => v.CallRecordId == callRecordId && v.VerifiedBy == ebillUser.IndexNumber);

                    if (existingVerification != null)
                    {
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

                // Submit to supervisor
                var submittedCount = await _verificationService.SubmitToSupervisorAsync(
                    verificationIds,
                    ebillUser.IndexNumber);

                StatusMessage = $"Successfully submitted {submittedCount} call verifications to your supervisor for approval.";
                if (hasOverage)
                {
                    StatusMessage += " Your overage justification and supporting document have been included.";
                }
                StatusMessageClass = "success";
                return RedirectToPage("/Modules/EBillManagement/CallRecords/MyCallLogs");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error submitting verifications: {ex.Message}";
                StatusMessageClass = "danger";
                return RedirectToPage(new { ids = string.Join(",", callRecordIds) });
            }
        }
    }
}
