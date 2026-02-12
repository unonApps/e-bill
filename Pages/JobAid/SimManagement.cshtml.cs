using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TAB.Web.Pages.JobAid;

[Authorize]
public class SimManagementModel : PageModel
{
    public void OnGet() { }
}
