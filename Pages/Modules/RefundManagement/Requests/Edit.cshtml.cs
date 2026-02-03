using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Pages.Modules.RefundManagement.Requests
{
    [Authorize]
    public class EditModel : RefundRequestFormModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<EditModel> _logger;

        public EditModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment,
            ILogger<EditModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // Check if user is admin
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            // Load the refund request
            var query = _context.RefundRequests.AsQueryable();

            // If not admin, filter by user (RequestedBy stores the user ID)
            if (!isAdmin)
            {
                query = query.Where(r => r.RequestedBy == currentUser.Id);
            }

            RefundRequest = await query.FirstOrDefaultAsync(r => r.Id == id);

            if (RefundRequest == null)
            {
                return NotFound();
            }

            // Only allow editing draft requests
            if (RefundRequest.Status != RefundRequestStatus.Draft)
            {
                TempData["ErrorMessage"] = "Only draft requests can be edited.";
                return RedirectToPage("./Index");
            }

            // Set the organization ID for office dropdown
            if (!string.IsNullOrEmpty(RefundRequest.Organization))
            {
                var organization = await _context.Organizations
                    .FirstOrDefaultAsync(o => o.Name == RefundRequest.Organization);
                if (organization != null)
                {
                    PreSelectedOrganizationId = organization.Id;
                }
            }

            // Load dropdown data
            await LoadOrganizationsAsync();
            await LoadClassesOfServiceAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string action, IFormFile? receiptFile)
        {
            _logger.LogInformation("=== EDIT FORM SUBMISSION ===");
            _logger.LogInformation("Action: {Action}", action);
            _logger.LogInformation("Request ID: {Id}", RefundRequest.Id);
            _logger.LogInformation("Receipt file: {FileName}", receiptFile?.FileName ?? "No new file");

            // Verify the request exists and belongs to the user
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            var existingRequest = await _context.RefundRequests
                .FirstOrDefaultAsync(r => r.Id == RefundRequest.Id);

            if (existingRequest == null)
            {
                return NotFound();
            }

            // Check ownership
            if (!isAdmin && existingRequest.RequestedBy != currentUser.Id)
            {
                return Forbid();
            }

            // Only allow editing draft requests
            if (existingRequest.Status != RefundRequestStatus.Draft)
            {
                TempData["ErrorMessage"] = "Only draft requests can be edited.";
                return RedirectToPage("./Index");
            }

            // Handle file upload if a new file is provided
            if (receiptFile != null && receiptFile.Length > 0)
            {
                // Allow PDF and common image formats
                var allowedContentTypes = new[] {
                    "application/pdf",
                    "image/jpeg",
                    "image/jpg",
                    "image/png"
                };

                if (!allowedContentTypes.Contains(receiptFile.ContentType.ToLower()))
                {
                    ModelState.AddModelError("ReceiptFile", "Only PDF, JPG, and PNG files are allowed.");
                }
                else if (receiptFile.Length > 10 * 1024 * 1024) // 10MB limit
                {
                    ModelState.AddModelError("ReceiptFile", "File size cannot exceed 10MB.");
                }
                else
                {
                    // Save the file to database as binary data
                    using (var memoryStream = new MemoryStream())
                    {
                        await receiptFile.CopyToAsync(memoryStream);
                        RefundRequest.PurchaseReceiptData = memoryStream.ToArray();
                    }

                    RefundRequest.PurchaseReceiptFileName = receiptFile.FileName;
                    RefundRequest.PurchaseReceiptContentType = receiptFile.ContentType;
                    RefundRequest.PurchaseReceiptUploadDate = DateTime.UtcNow;
                    RefundRequest.PurchaseReceiptPath = $"database:{receiptFile.FileName}";

                    _logger.LogInformation("New receipt file '{FileName}' ({Size} bytes) will replace existing file",
                        receiptFile.FileName, RefundRequest.PurchaseReceiptData.Length);
                }
            }
            else
            {
                // No new file uploaded - preserve existing file data
                RefundRequest.PurchaseReceiptData = existingRequest.PurchaseReceiptData;
                RefundRequest.PurchaseReceiptFileName = existingRequest.PurchaseReceiptFileName;
                RefundRequest.PurchaseReceiptContentType = existingRequest.PurchaseReceiptContentType;
                RefundRequest.PurchaseReceiptUploadDate = existingRequest.PurchaseReceiptUploadDate;
                RefundRequest.PurchaseReceiptPath = existingRequest.PurchaseReceiptPath;
            }

            // Handle validation based on action type
            if (action == "draft")
            {
                // For drafts, clear all validation errors to allow partial saves
                ModelState.Clear();

                // Set default values for required database fields to prevent NULL constraint violations
                if (string.IsNullOrEmpty(RefundRequest.PrimaryMobileNumber))
                    RefundRequest.PrimaryMobileNumber = "";
                if (string.IsNullOrEmpty(RefundRequest.IndexNo))
                    RefundRequest.IndexNo = "";
                if (string.IsNullOrEmpty(RefundRequest.MobileNumberAssignedTo))
                    RefundRequest.MobileNumberAssignedTo = "";
                if (string.IsNullOrEmpty(RefundRequest.Office))
                    RefundRequest.Office = "";
                if (string.IsNullOrEmpty(RefundRequest.MobileService))
                    RefundRequest.MobileService = "";
                if (string.IsNullOrEmpty(RefundRequest.ClassOfService))
                    RefundRequest.ClassOfService = "";
                if (string.IsNullOrEmpty(RefundRequest.DevicePurchaseCurrency))
                    RefundRequest.DevicePurchaseCurrency = "";
                if (string.IsNullOrEmpty(RefundRequest.Organization))
                    RefundRequest.Organization = "";
                if (string.IsNullOrEmpty(RefundRequest.UmojaBankName))
                    RefundRequest.UmojaBankName = "";
                if (string.IsNullOrEmpty(RefundRequest.Supervisor))
                    RefundRequest.Supervisor = "";
                if (string.IsNullOrEmpty(RefundRequest.PurchaseReceiptPath))
                    RefundRequest.PurchaseReceiptPath = "";

                // Set default values for numeric fields
                if (RefundRequest.DeviceAllowance == 0)
                    RefundRequest.DeviceAllowance = 0.01m;
                if (RefundRequest.DevicePurchaseAmount == 0)
                    RefundRequest.DevicePurchaseAmount = 0.01m;
            }
            else if (action == "submit")
            {
                // For submit, validate required fields
                if (receiptFile == null && string.IsNullOrEmpty(RefundRequest.PurchaseReceiptPath))
                {
                    ModelState.AddModelError("ReceiptFile", "Purchase receipt is required for submission.");
                }
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid for RefundRequest edit");
                foreach (var modelState in ModelState)
                {
                    if (modelState.Value?.Errors.Count > 0)
                    {
                        foreach (var error in modelState.Value.Errors)
                        {
                            _logger.LogWarning("Validation error for field {Field}: {Error}",
                                modelState.Key, error.ErrorMessage);
                        }
                    }
                }

                // Preserve the organization ID for office dropdown on error
                if (!string.IsNullOrEmpty(RefundRequest.Organization))
                {
                    var organization = await _context.Organizations
                        .FirstOrDefaultAsync(o => o.Name == RefundRequest.Organization);
                    if (organization != null)
                    {
                        PreSelectedOrganizationId = organization.Id;
                    }
                }

                await LoadOrganizationsAsync();
                await LoadClassesOfServiceAsync();
                return Page();
            }

            // Preserve system fields from existing request
            RefundRequest.RequestedBy = existingRequest.RequestedBy;
            RefundRequest.RequestDate = existingRequest.RequestDate;

            // Set supervisor email
            if (!string.IsNullOrEmpty(RefundRequest.Supervisor))
            {
                RefundRequest.SupervisorEmail = RefundRequest.Supervisor;

                // If supervisor name is not provided, try to get it from the database
                if (string.IsNullOrEmpty(RefundRequest.SupervisorName))
                {
                    var supervisorUser = await _context.Users
                        .FirstOrDefaultAsync(u => u.Email == RefundRequest.SupervisorEmail);
                    if (supervisorUser != null)
                    {
                        RefundRequest.SupervisorName = $"{supervisorUser.FirstName} {supervisorUser.LastName}";
                    }
                }
            }

            // Update status based on action
            if (action == "submit")
            {
                RefundRequest.Status = RefundRequestStatus.PendingSupervisor;
                RefundRequest.SubmittedToSupervisor = true;
                StatusMessage = "Your mobile device reimbursement request has been updated and submitted.";
            }
            else
            {
                RefundRequest.Status = RefundRequestStatus.Draft;
                RefundRequest.SubmittedToSupervisor = false;
                StatusMessage = "Your mobile device reimbursement request has been updated.";
            }

            try
            {
                _context.RefundRequests.Update(RefundRequest);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Refund request {RequestId} updated successfully", RefundRequest.Id);

                return RedirectToPage("./Index", new { message = StatusMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating refund request {RequestId}", RefundRequest.Id);
                ModelState.AddModelError("", "An error occurred while updating your request. Please try again.");

                // Preserve the organization ID for office dropdown on error
                if (!string.IsNullOrEmpty(RefundRequest.Organization))
                {
                    var organization = await _context.Organizations
                        .FirstOrDefaultAsync(o => o.Name == RefundRequest.Organization);
                    if (organization != null)
                    {
                        PreSelectedOrganizationId = organization.Id;
                    }
                }

                await LoadOrganizationsAsync();
                await LoadClassesOfServiceAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnGetOfficesByOrganizationAsync(int organizationId)
        {
            try
            {
                var offices = await _context.Offices
                    .Where(o => o.OrganizationId == organizationId)
                    .OrderBy(o => o.Name)
                    .Select(o => new { id = o.Id, name = o.Name })
                    .ToListAsync();

                return new JsonResult(new { success = true, offices = offices });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving offices for organization {OrganizationId}", organizationId);
                return new JsonResult(new { success = false, message = "Error retrieving offices" });
            }
        }

        private async Task LoadOrganizationsAsync()
        {
            Organizations = await _context.Organizations
                .OrderBy(o => o.Name)
                .ToListAsync();
        }

        private async Task LoadClassesOfServiceAsync()
        {
            ClassesOfService = await _context.ClassOfServices
                .Where(c => c.ServiceStatus == ServiceStatus.Active)
                .OrderBy(c => c.Class)
                .ThenBy(c => c.Service)
                .ToListAsync();
        }
    }
}
