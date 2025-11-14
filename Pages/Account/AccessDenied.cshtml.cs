using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TAB.Web.Pages.Account
{
    public class AccessDeniedModel : PageModel
    {
        public string? Reason { get; set; }
        public string Message { get; set; } = "You do not have access to this resource.";

        public void OnGet(string? reason)
        {
            Reason = reason;

            // Check for custom error message from TempData
            if (TempData["ErrorMessage"] is string errorMessage)
            {
                Message = errorMessage;
            }
            else if (reason == "notProvisioned")
            {
                Message = "Your account has not been provisioned in the system. Please contact your administrator to request access.";
            }
        }
    }
} 