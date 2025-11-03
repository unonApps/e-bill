using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TAB.Web.Models.Enums;

namespace TAB.Web.Models
{
    /// <summary>
    /// Audit trail for all recovery actions performed on call records
    /// </summary>
    [Table("RecoveryLogs")]
    public class RecoveryLog
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to the call record this recovery applies to
        /// </summary>
        public int CallRecordId { get; set; }

        /// <summary>
        /// Navigation property to the call record
        /// </summary>
        [ForeignKey("CallRecordId")]
        public virtual CallRecord? CallRecord { get; set; }

        /// <summary>
        /// Batch ID this recovery is associated with
        /// </summary>
        public Guid BatchId { get; set; }

        /// <summary>
        /// Navigation property to the staging batch
        /// </summary>
        [ForeignKey("BatchId")]
        public virtual StagingBatch? StagingBatch { get; set; }

        /// <summary>
        /// Type of recovery (StaffNonVerification, SupervisorNonApproval, etc.)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string RecoveryType { get; set; } = string.Empty;

        /// <summary>
        /// Action taken (Personal, Official, ClassOfService)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string RecoveryAction { get; set; } = string.Empty;

        /// <summary>
        /// When the recovery was processed
        /// </summary>
        [Required]
        public DateTime RecoveryDate { get; set; }

        /// <summary>
        /// Detailed reason for this recovery action
        /// </summary>
        [Required]
        [MaxLength(1000)]
        public string RecoveryReason { get; set; } = string.Empty;

        /// <summary>
        /// Amount recovered from this call
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountRecovered { get; set; }

        /// <summary>
        /// Index number of staff/supervisor from whom amount is recovered
        /// </summary>
        [MaxLength(100)]
        public string? RecoveredFrom { get; set; }

        /// <summary>
        /// Who/what processed this recovery (System-AutoRecovery, admin email, etc.)
        /// </summary>
        [MaxLength(100)]
        public string? ProcessedBy { get; set; }

        /// <summary>
        /// The deadline that was missed (if applicable)
        /// </summary>
        public DateTime? DeadlineDate { get; set; }

        /// <summary>
        /// Whether this recovery was automated or manual
        /// </summary>
        public bool IsAutomated { get; set; } = true;

        /// <summary>
        /// Additional metadata in JSON format
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? Metadata { get; set; }
    }
}
