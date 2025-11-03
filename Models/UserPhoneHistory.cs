using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAB.Web.Models
{
    public class UserPhoneHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserPhoneId { get; set; }

        [ForeignKey("UserPhoneId")]
        public virtual UserPhone? UserPhone { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty; // Created, Updated, Deleted, Assigned, Unassigned, SetPrimary, LineTypeChanged, StatusChanged

        [MaxLength(100)]
        public string? FieldChanged { get; set; } // Which field was changed (PhoneNumber, PhoneType, LineType, IsPrimary, Status, etc.)

        [MaxLength(500)]
        public string? OldValue { get; set; } // Previous value

        [MaxLength(500)]
        public string? NewValue { get; set; } // New value

        [MaxLength(1000)]
        public string? Description { get; set; } // Human-readable description of the change

        [MaxLength(200)]
        public string? ChangedBy { get; set; } // Who made the change

        [Required]
        public DateTime ChangedDate { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? IPAddress { get; set; } // Optional: track IP address of change

        [MaxLength(500)]
        public string? UserAgent { get; set; } // Optional: track browser/client info

        // Computed properties for display
        [NotMapped]
        public string ActionBadgeColor => Action switch
        {
            "Created" => "success",
            "Assigned" => "success",
            "Updated" => "info",
            "LineTypeChanged" => "primary",
            "SetPrimary" => "warning",
            "StatusChanged" => "secondary",
            "Unassigned" => "danger",
            "Deleted" => "danger",
            _ => "secondary"
        };

        [NotMapped]
        public string ActionIcon => Action switch
        {
            "Created" => "bi-plus-circle",
            "Assigned" => "bi-link-45deg",
            "Updated" => "bi-pencil",
            "LineTypeChanged" => "bi-tag",
            "SetPrimary" => "bi-star",
            "StatusChanged" => "bi-toggle-on",
            "Unassigned" => "bi-x-circle",
            "Deleted" => "bi-trash",
            _ => "bi-info-circle"
        };

        [NotMapped]
        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.UtcNow - ChangedDate;

                if (timeSpan.TotalMinutes < 1)
                    return "just now";
                if (timeSpan.TotalMinutes < 60)
                    return $"{(int)timeSpan.TotalMinutes}m ago";
                if (timeSpan.TotalHours < 24)
                    return $"{(int)timeSpan.TotalHours}h ago";
                if (timeSpan.TotalDays < 7)
                    return $"{(int)timeSpan.TotalDays}d ago";
                if (timeSpan.TotalDays < 30)
                    return $"{(int)(timeSpan.TotalDays / 7)}w ago";
                if (timeSpan.TotalDays < 365)
                    return $"{(int)(timeSpan.TotalDays / 30)}mo ago";

                return $"{(int)(timeSpan.TotalDays / 365)}y ago";
            }
        }
    }
}
