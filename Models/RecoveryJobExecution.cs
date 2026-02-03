using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAB.Web.Models
{
    /// <summary>
    /// Tracks execution history of the recovery automation job
    /// </summary>
    [Table("RecoveryJobExecutions")]
    public class RecoveryJobExecution
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// When the job execution started
        /// </summary>
        [Required]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// When the job execution completed
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Duration of the job in milliseconds
        /// </summary>
        public long? DurationMs { get; set; }

        /// <summary>
        /// Status of the job execution (Running, Completed, Failed)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Running";

        /// <summary>
        /// Type of job run (Automatic, Manual)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string RunType { get; set; } = "Automatic";

        /// <summary>
        /// User who triggered manual run (if applicable)
        /// </summary>
        [MaxLength(100)]
        public string? TriggeredBy { get; set; }

        /// <summary>
        /// Number of expired verification deadlines processed
        /// </summary>
        public int ExpiredVerificationsProcessed { get; set; }

        /// <summary>
        /// Number of expired approval deadlines processed
        /// </summary>
        public int ExpiredApprovalsProcessed { get; set; }

        /// <summary>
        /// Number of reverted verifications processed
        /// </summary>
        public int RevertedVerificationsProcessed { get; set; }

        /// <summary>
        /// Total number of call records processed
        /// </summary>
        public int TotalRecordsProcessed { get; set; }

        /// <summary>
        /// Total amount recovered (legacy - sum of both currencies, kept for backwards compatibility)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmountRecovered { get; set; }

        /// <summary>
        /// Total amount recovered in KSH (Safaricom, Airtel, PSTN)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmountRecoveredKSH { get; set; }

        /// <summary>
        /// Total amount recovered in USD (Private Wire)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmountRecoveredUSD { get; set; }

        /// <summary>
        /// Number of deadline reminders sent
        /// </summary>
        public int RemindersSent { get; set; }

        /// <summary>
        /// Error message if job failed
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Detailed execution log
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? ExecutionLog { get; set; }

        /// <summary>
        /// Next scheduled run time
        /// </summary>
        public DateTime? NextScheduledRun { get; set; }
    }

    /// <summary>
    /// Status of recovery job execution
    /// </summary>
    public enum JobExecutionStatus
    {
        Running,
        Completed,
        Failed,
        Cancelled
    }
}
