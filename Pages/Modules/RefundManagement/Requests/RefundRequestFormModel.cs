using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TAB.Web.Models;

namespace TAB.Web.Pages.Modules.RefundManagement.Requests
{
    public abstract class RefundRequestFormModel : PageModel
    {
        [BindProperty]
        public RefundRequest RefundRequest { get; set; } = new RefundRequest();

        public List<Organization> Organizations { get; set; } = new List<Organization>();
        public List<ClassOfService> ClassesOfService { get; set; } = new List<ClassOfService>();
        public int? PreSelectedOrganizationId { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }
    }
}
