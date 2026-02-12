using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TAB.Web.Pages.JobAid;

[Authorize]
public class RefundManagementModel : PageModel
{
    public void OnGet() { }
}
