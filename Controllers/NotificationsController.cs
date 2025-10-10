using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TAB.Web.Models;
using TAB.Web.Services;

namespace TAB.Web.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsController(
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var count = await _notificationService.GetUnreadCountAsync(user.Id);
            return Ok(new { count });
        }

        [HttpPost("{id}/markasread")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var result = await _notificationService.MarkAsReadAsync(id, user.Id);

            if (result)
                return Ok(new { success = true });

            return BadRequest(new { success = false, message = "Failed to mark notification as read" });
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentNotifications([FromQuery] int limit = 5)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var notifications = await _notificationService.GetUnreadNotificationsAsync(user.Id, limit);

            return Ok(new
            {
                notifications = notifications.Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Message,
                    n.Type,
                    n.Icon,
                    n.Link,
                    n.IsRead,
                    n.CreatedDate,
                    TimeAgo = GetTimeAgo(n.CreatedDate)
                })
            });
        }

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays}d ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)(timeSpan.TotalDays / 7)}w ago";

            return dateTime.ToString("MMM dd");
        }
    }
}
