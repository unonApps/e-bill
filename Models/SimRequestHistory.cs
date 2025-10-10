using System.ComponentModel.DataAnnotations;

namespace TAB.Web.Models
{
    public class SimRequestHistory
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "SIM Request ID")]
        public int SimRequestId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Action")]
        public string Action { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Previous Status")]
        public string? PreviousStatus { get; set; }

        [StringLength(50)]
        [Display(Name = "New Status")]
        public string? NewStatus { get; set; }

        [StringLength(1000)]
        [Display(Name = "Comments")]
        public string? Comments { get; set; }

        [Required]
        [StringLength(450)]
        [Display(Name = "Performed By")]
        public string PerformedBy { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "User Name")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        [Display(Name = "IP Address")]
        public string? IpAddress { get; set; }

        // Navigation property
        public virtual SimRequest? SimRequest { get; set; }
    }

    public static class HistoryActions
    {
        public const string Created = "Created";
        public const string Updated = "Updated";
        public const string StatusChanged = "Status Changed";
        public const string SubmittedToSupervisor = "Submitted to Supervisor";
        public const string SupervisorApproved = "Supervisor Approved";
        public const string SupervisorRejected = "Supervisor Rejected";
        public const string SupervisorReverted = "Supervisor Reverted";
        public const string AdminApproved = "Admin Approved";
        public const string AdminRejected = "Admin Rejected";
        public const string IctsProcessed = "ICTS Processed";
        public const string IctsReverted = "ICTS Reverted to Requestor";
        public const string IctsNewSimApproved = "ICTS New SIM Approved";
        public const string IctsCollectionNotified = "ICTS Collection Notified";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
        public const string CommentAdded = "Comment Added";
    }
} 