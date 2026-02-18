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
    public class ViewModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ViewModel> _logger;

        public ViewModel(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ViewModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public RefundRequest RefundRequest { get; set; } = null!;
        public bool IsAdmin { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            // Check if user is admin
            IsAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            // Load the refund request
            var query = _context.RefundRequests.AsQueryable();

            // If not admin, filter by user (RequestedBy stores the user ID)
            if (!IsAdmin)
            {
                query = query.Where(r => r.RequestedBy == currentUser.Id);
            }

            RefundRequest = await query.FirstOrDefaultAsync(r => r.PublicId == id);

            if (RefundRequest == null)
            {
                return NotFound();
            }

            return Page();
        }
    }
}