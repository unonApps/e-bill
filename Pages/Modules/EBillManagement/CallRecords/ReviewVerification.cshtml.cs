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
    public class ReviewVerificationModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICallLogVerificationService _verificationService;

        public ReviewVerificationModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ICallLogVerificationService verificationService)
        {
            _context = context;
            _userManager = userManager;
            _verificationService = verificationService;
        }

        public List<CallLogVerification> Verifications { get; set; } = new();
        public string? UserIndexNumber { get; set; }
        public string? UserName { get; set; }
        public DateTime? SubmittedDate { get; set; }
        public string? SupervisorIndexNumber { get; set; }
        public List<int>? VerificationIdsFilter { get; set; }

        public decimal TotalAmount { get; set; }
        public int TotalCalls { get; set; }
        public int OverageCount { get; set; }
        public decimal AllowanceLimit { get; set; }
        public decimal TotalOverage { get; set; }

        [BindProperty]
        public string? SupervisorComments { get; set; }

        [BindProperty]
        public string? ActionReason { get; set; }

        [BindProperty]
        public List<PartialApprovalInput> PartialApprovals { get; set; } = new();

        [BindProperty]
        public string? PartialApprovalsJson { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        public class PartialApprovalInput
        {
            public int VerificationId { get; set; }
            public decimal ApprovedAmount { get; set; }
            public string? Reason { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(string indexNumber, string date, string? ids)
        {
            if (string.IsNullOrEmpty(indexNumber) || string.IsNullOrEmpty(date))
            {
                StatusMessage = "Invalid parameters.";
                StatusMessageClass = "danger";
                return RedirectToPage("/Modules/EBillManagement/CallRecords/SupervisorApprovals");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            // Use the logged-in user's email as supervisor identifier
            SupervisorIndexNumber = user.Email;
            UserIndexNumber = indexNumber;

            if (DateTime.TryParse(date, out var submittedDate))
            {
                SubmittedDate = submittedDate;
            }

            // Parse optional verification IDs filter
            if (!string.IsNullOrEmpty(ids))
            {
                VerificationIdsFilter = ids.Split(',')
                    .Select(id => int.TryParse(id.Trim(), out var parsedId) ? parsedId : 0)
                    .Where(id => id > 0)
                    .ToList();
            }

            await LoadVerificationsAsync();

            return Page();
        }

        private async Task LoadVerificationsAsync()
        {
            if (string.IsNullOrEmpty(UserIndexNumber) || !SubmittedDate.HasValue)
                return;

            var startDate = SubmittedDate.Value.Date;
            var endDate = startDate.AddDays(1);

            var query = _context.CallLogVerifications
                .Include(v => v.CallRecord)
                    .ThenInclude(c => c.UserPhone)
                        .ThenInclude(up => up.ClassOfService)
                .Include(v => v.Documents)
                .Include(v => v.PaymentAssignment)
                .Where(v => v.VerifiedBy == UserIndexNumber
                    && v.SubmittedToSupervisor
                    && v.SubmittedDate >= startDate
                    && v.SubmittedDate < endDate
                    && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"));

            // If specific verification IDs are provided, filter to only those
            if (VerificationIdsFilter != null && VerificationIdsFilter.Any())
            {
                query = query.Where(v => VerificationIdsFilter.Contains(v.Id));
            }

            Verifications = await query
                .OrderBy(v => v.CallRecord.CallDate)
                .ToListAsync();

            if (Verifications.Any())
            {
                TotalCalls = Verifications.Count;
                TotalAmount = Verifications.Sum(v => v.ActualAmount);
                OverageCount = Verifications.Count(v => v.IsOverage);
                TotalOverage = Verifications.Where(v => v.IsOverage).Sum(v => v.OverageAmount);

                // Get user info
                var ebillUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.IndexNumber == UserIndexNumber);

                if (ebillUser != null)
                {
                    UserName = $"{ebillUser.FirstName} {ebillUser.LastName}";

                    // Get allowance limit from primary phone's class of service
                    var primaryPhone = await _context.UserPhones
                        .Include(up => up.ClassOfService)
                        .FirstOrDefaultAsync(p => p.IndexNumber == UserIndexNumber
                            && p.IsPrimary
                            && p.IsActive);

                    if (primaryPhone?.ClassOfService != null)
                    {
                        // AirtimeAllowanceAmount represents the total monthly allowance (includes airtime AND data)
                        // NULL or 0 means Unlimited
                        AllowanceLimit = primaryPhone.ClassOfService.AirtimeAllowanceAmount ?? 0;
                    }
                }
            }
        }

        public async Task<IActionResult> OnPostApproveAllAsync(string indexNumber, string date)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            if (DateTime.TryParse(date, out var submittedDate))
            {
                var startDate = submittedDate.Date;
                var endDate = startDate.AddDays(1);

                var verificationIds = await _context.CallLogVerifications
                    .Where(v => v.VerifiedBy == indexNumber
                        && v.SubmittedToSupervisor
                        && v.SubmittedDate >= startDate
                        && v.SubmittedDate < endDate
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"))
                    .Select(v => v.Id)
                    .ToListAsync();

                int successCount = 0;
                foreach (var verificationId in verificationIds)
                {
                    var result = await _verificationService.ApproveVerificationAsync(
                        verificationId,
                        user.Email,
                        null, // Full amount
                        SupervisorComments
                    );
                    if (result) successCount++;
                }

                StatusMessage = $"Successfully approved {successCount} call verification(s).";
                StatusMessageClass = "success";
            }

            return RedirectToPage("/Modules/EBillManagement/CallRecords/SupervisorApprovals");
        }

        public async Task<IActionResult> OnPostApprovePartiallyAsync(string indexNumber, string date)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            // Deserialize JSON if provided
            if (!string.IsNullOrEmpty(PartialApprovalsJson))
            {
                try
                {
                    PartialApprovals = System.Text.Json.JsonSerializer.Deserialize<List<PartialApprovalInput>>(PartialApprovalsJson) ?? new();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error parsing approval data: {ex.Message}";
                    StatusMessageClass = "danger";
                    return RedirectToPage(new { indexNumber, date });
                }
            }

            if (PartialApprovals == null || !PartialApprovals.Any())
            {
                StatusMessage = "No partial approvals specified.";
                StatusMessageClass = "warning";
                return RedirectToPage(new { indexNumber, date });
            }

            int successCount = 0;
            foreach (var partial in PartialApprovals)
            {
                var result = await _verificationService.ApproveVerificationAsync(
                    partial.VerificationId,
                    user.Email,
                    partial.ApprovedAmount, // Partial amount
                    partial.Reason ?? SupervisorComments
                );
                if (result) successCount++;
            }

            StatusMessage = $"Successfully processed {successCount} partial approval(s).";
            StatusMessageClass = "success";

            return RedirectToPage("/Modules/EBillManagement/CallRecords/SupervisorApprovals");
        }

        public async Task<IActionResult> OnPostRejectAsync(string indexNumber, string date)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            if (string.IsNullOrWhiteSpace(ActionReason))
            {
                StatusMessage = "Rejection reason is required.";
                StatusMessageClass = "warning";
                return RedirectToPage(new { indexNumber, date });
            }

            if (DateTime.TryParse(date, out var submittedDate))
            {
                var startDate = submittedDate.Date;
                var endDate = startDate.AddDays(1);

                var verificationIds = await _context.CallLogVerifications
                    .Where(v => v.VerifiedBy == indexNumber
                        && v.SubmittedToSupervisor
                        && v.SubmittedDate >= startDate
                        && v.SubmittedDate < endDate
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"))
                    .Select(v => v.Id)
                    .ToListAsync();

                int successCount = 0;
                foreach (var verificationId in verificationIds)
                {
                    var result = await _verificationService.RejectVerificationAsync(
                        verificationId,
                        user.Email,
                        ActionReason
                    );
                    if (result) successCount++;
                }

                StatusMessage = $"Rejected {successCount} call verification(s).";
                StatusMessageClass = "info";
            }

            return RedirectToPage("/Modules/EBillManagement/CallRecords/SupervisorApprovals");
        }

        public async Task<IActionResult> OnPostRevertAsync(string indexNumber, string date)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            if (string.IsNullOrWhiteSpace(ActionReason))
            {
                StatusMessage = "Revert reason is required.";
                StatusMessageClass = "warning";
                return RedirectToPage(new { indexNumber, date });
            }

            if (DateTime.TryParse(date, out var submittedDate))
            {
                var startDate = submittedDate.Date;
                var endDate = startDate.AddDays(1);

                var verificationIds = await _context.CallLogVerifications
                    .Where(v => v.VerifiedBy == indexNumber
                        && v.SubmittedToSupervisor
                        && v.SubmittedDate >= startDate
                        && v.SubmittedDate < endDate
                        && (v.SupervisorApprovalStatus == null || v.SupervisorApprovalStatus == "Pending"))
                    .Select(v => v.Id)
                    .ToListAsync();

                int successCount = 0;
                foreach (var verificationId in verificationIds)
                {
                    var result = await _verificationService.RevertVerificationAsync(
                        verificationId,
                        user.Email,
                        ActionReason
                    );
                    if (result) successCount++;
                }

                StatusMessage = $"Reverted {successCount} call verification(s) back to user for revision.";
                StatusMessageClass = "info";
            }

            return RedirectToPage("/Modules/EBillManagement/CallRecords/SupervisorApprovals");
        }
    }
}
