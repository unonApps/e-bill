using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Pages.Modules.EBillManagement.Approvals
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int SupervisorPendingCount { get; set; }
        public List<Ebill> Ebills { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var supervisorName = $"{User.FindFirst("FirstName")?.Value} {User.FindFirst("LastName")?.Value}";

            // Get e-bills for supervisor
            Ebills = await _context.Ebills
                .Where(e => e.SupervisorName == supervisorName)
                .OrderByDescending(e => e.RequestDate)
                .ToListAsync();

            SupervisorPendingCount = Ebills.Count(e => e.Status == EbillStatus.PendingSupervisor);

            // Pass data to ViewData for partial views
            ViewData["Ebills"] = Ebills;

            return Page();
        }
    }
}