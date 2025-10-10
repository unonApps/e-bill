using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TAB.Web.Models.Enums;

namespace TAB.Web.Models
{
    public class CallLogPaymentAssignment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CallRecordId { get; set; }

        // Assignment Details
        [Required]
        [MaxLength(50)]
        public string AssignedFrom { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string AssignedTo { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string AssignmentReason { get; set; } = string.Empty;

        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

        // Acceptance
        [Required]
        [MaxLength(20)]
        public string AssignmentStatus { get; set; } = "Pending";
        // Values: Pending, Accepted, Rejected, Reassigned

        public DateTime? AcceptedDate { get; set; }

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        // Notification Tracking
        public bool NotificationSent { get; set; } = false;

        public DateTime? NotificationSentDate { get; set; }

        public DateTime? NotificationViewedDate { get; set; }

        // Audit
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        // Navigation Properties
        [ForeignKey("CallRecordId")]
        public virtual CallRecord CallRecord { get; set; } = null!;

        // Helper Properties
        [NotMapped]
        public bool IsPending => AssignmentStatus == "Pending";

        [NotMapped]
        public bool IsAccepted => AssignmentStatus == "Accepted";

        [NotMapped]
        public bool IsRejected => AssignmentStatus == "Rejected";
    }
}
