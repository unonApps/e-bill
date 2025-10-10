using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TAB.Web.Models;
using TAB.Web.Services;

namespace TAB.Web.Pages.Notifications
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexModel(
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        public List<Notification> Notifications { get; set; } = new();
        public int UnreadCount { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Filter { get; set; } // all, unread, read

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 20;
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        [TempData]
        public string? StatusMessageClass { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            UnreadCount = await _notificationService.GetUnreadCountAsync(user.Id);

            // Get all notifications with filter
            var allNotifications = await _notificationService.GetUserNotificationsAsync(user.Id, 1, 1000);

            // Apply filter
            var filteredNotifications = Filter switch
            {
                "unread" => allNotifications.Where(n => !n.IsRead).ToList(),
                "read" => allNotifications.Where(n => n.IsRead).ToList(),
                _ => allNotifications
            };

            TotalRecords = filteredNotifications.Count;
            TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);

            // Apply pagination
            Notifications = filteredNotifications
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return Page();
        }

        public async Task<IActionResult> OnPostMarkAsReadAsync(int notificationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var result = await _notificationService.MarkAsReadAsync(notificationId, user.Id);

            if (result)
            {
                StatusMessage = "Notification marked as read.";
                StatusMessageClass = "success";
            }
            else
            {
                StatusMessage = "Failed to mark notification as read.";
                StatusMessageClass = "danger";
            }

            return RedirectToPage(new { Filter, PageNumber });
        }

        public async Task<IActionResult> OnPostMarkAllAsReadAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var count = await _notificationService.MarkAllAsReadAsync(user.Id);

            StatusMessage = $"Marked {count} notification(s) as read.";
            StatusMessageClass = "success";

            return RedirectToPage(new { Filter = "all", PageNumber = 1 });
        }

        public async Task<IActionResult> OnPostDeleteAsync(int notificationId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var result = await _notificationService.DeleteNotificationAsync(notificationId, user.Id);

            if (result)
            {
                StatusMessage = "Notification deleted.";
                StatusMessageClass = "success";
            }
            else
            {
                StatusMessage = "Failed to delete notification.";
                StatusMessageClass = "danger";
            }

            return RedirectToPage(new { Filter, PageNumber });
        }

        public async Task<IActionResult> OnPostDeleteAllReadAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var count = await _notificationService.DeleteAllReadAsync(user.Id);

            StatusMessage = $"Deleted {count} read notification(s).";
            StatusMessageClass = "success";

            return RedirectToPage(new { Filter = "all", PageNumber = 1 });
        }
    }
}
