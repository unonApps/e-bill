using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TAB.Web.Models.Enums;

namespace TAB.Web.Models
{
    /// <summary>
    /// Tracks all deadlines for verification and approval processes
    /// </summary>
    [Table("DeadlineTracking")]
    public class DeadlineTracking
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Batch ID this deadline is associated with
        /// </summary>
        public Guid BatchId { get; set; }

        /// <summary>
        /// Navigation property to the staging batch
        /// </summary>
        [ForeignKey("BatchId")]
        public virtual StagingBatch? StagingBatch { get; set; }

        /// <summary>
        /// Type of deadline (Verification, Approval, ReVerification)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string DeadlineType { get; set; } = string.Empty;

        /// <summary>
        /// Who this deadline applies to (IndexNumber or "AllSupervisors")
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string TargetEntity { get; set; } = string.Empty;

        /// <summary>
        /// The original deadline date
        /// </summary>
        [Required]
        public DateTime DeadlineDate { get; set; }

        /// <summary>
        /// Extended deadline if approved
        /// </summary>
        public DateTime? ExtendedDeadline { get; set; }

        /// <summary>
        /// Current status of the deadline
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string DeadlineStatus { get; set; } = "Pending";

        /// <summary>
        /// When the deadline was missed
        /// </summary>
        public DateTime? MissedDate { get; set; }

        /// <summary>
        /// Whether recovery has been processed for this deadline
        /// </summary>
        public bool RecoveryProcessed { get; set; } = false;

        /// <summary>
        /// When recovery was processed
        /// </summary>
        public DateTime? RecoveryProcessedDate { get; set; }

        /// <summary>
        /// Reason for deadline extension
        /// </summary>
        [MaxLength(500)]
        public string? ExtensionReason { get; set; }

        /// <summary>
        /// Who approved the extension
        /// </summary>
        [MaxLength(100)]
        public string? ExtensionApprovedBy { get; set; }

        /// <summary>
        /// When the extension was approved
        /// </summary>
        public DateTime? ExtensionApprovedDate { get; set; }

        /// <summary>
        /// When this deadline tracking record was created
        /// </summary>
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Who created this deadline
        /// </summary>
        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// Additional notes
        /// </summary>
        [MaxLength(1000)]
        public string? Notes { get; set; }
    }
}
