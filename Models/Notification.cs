using System;
using System.ComponentModel.DataAnnotations;

namespace TAB.Web.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty; // ApplicationUser.Id

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = NotificationType.Info.ToString(); // Info, Success, Warning, Error, Action

        public bool IsRead { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ReadDate { get; set; }

        [MaxLength(500)]
        public string? Link { get; set; } // URL to navigate when clicked

        [MaxLength(100)]
        public string? RelatedEntityType { get; set; } // e.g., "CallLogVerification", "SimRequest"

        [MaxLength(100)]
        public string? RelatedEntityId { get; set; } // ID of the related entity

        [MaxLength(50)]
        public string? Icon { get; set; } // Bootstrap icon class (e.g., "bi-check-circle")

        // Navigation property
        public virtual ApplicationUser? User { get; set; }
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error,
        Action // Requires user action
    }
}
