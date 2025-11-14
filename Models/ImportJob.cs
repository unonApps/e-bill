using System;
using System.ComponentModel.DataAnnotations;

namespace TAB.Web.Models
{
    public class ImportJob
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(500)]
        public string FileName { get; set; } = string.Empty;

        public long FileSize { get; set; }

        [Required]
        [MaxLength(50)]
        public string CallLogType { get; set; } = string.Empty; // Safaricom, Airtel, PSTN, PrivateWire

        public int BillingMonth { get; set; }

        public int BillingYear { get; set; }

        [MaxLength(50)]
        public string? DateFormat { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Queued"; // Queued, Processing, Completed, Failed, Cancelled

        public int? RecordsProcessed { get; set; }

        public int? RecordsSuccess { get; set; }

        public int? RecordsError { get; set; }

        public string? ErrorMessage { get; set; }

        [Required]
        [MaxLength(256)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? StartedDate { get; set; }

        public DateTime? CompletedDate { get; set; }

        // Hangfire job ID
        [MaxLength(100)]
        public string? HangfireJobId { get; set; }

        // Duration in seconds
        public int? DurationSeconds { get; set; }

        // Progress percentage (0-100)
        public int? ProgressPercentage { get; set; }

        // Additional metadata as JSON
        public string? Metadata { get; set; }
    }
}
