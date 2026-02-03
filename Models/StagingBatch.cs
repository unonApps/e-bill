using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAB.Web.Models
{
    public class StagingBatch
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string BatchName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string BatchType { get; set; } = "Manual"; // Manual/Scheduled/ADF

        // Statistics
        public int TotalRecords { get; set; }
        public int VerifiedRecords { get; set; }
        public int RejectedRecords { get; set; }
        public int PendingRecords { get; set; }
        public int RecordsWithAnomalies { get; set; }

        // Dates
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? StartProcessingDate { get; set; }
        public DateTime? EndProcessingDate { get; set; }

        // Recovery Tracking
        public DateTime? RecoveryProcessingDate { get; set; }

        [MaxLength(50)]
        public string? RecoveryStatus { get; set; } // Pending, InProgress, Completed

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalRecoveredAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalPersonalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalOfficialAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TotalClassOfServiceAmount { get; set; }

        // Status
        public BatchStatus BatchStatus { get; set; } = BatchStatus.Created;

        // User Info
        [MaxLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? VerifiedBy { get; set; }

        [MaxLength(100)]
        public string? PublishedBy { get; set; }

        // Additional Info
        [MaxLength(200)]
        public string? SourceSystems { get; set; } // Comma-separated list

        public string? Notes { get; set; }

        // Hangfire Job Tracking
        [MaxLength(100)]
        public string? HangfireJobId { get; set; } // Track background job for cleanup

        // Processing Progress Tracking
        [MaxLength(100)]
        public string? CurrentOperation { get; set; } // Current operation being performed (e.g., "Consolidating Safaricom records")

        public int ProcessingProgress { get; set; } // Progress percentage (0-100)

        // Error Tracking
        public string? FailureReason { get; set; } // Stores the error message if batch fails

        // Billing Period tracking
        public int? BillingPeriodId { get; set; }

        [MaxLength(20)]
        public string BatchCategory { get; set; } = "MONTHLY"; // MONTHLY, INTERIM, CORRECTION

        // Navigation Properties
        [ForeignKey("BillingPeriodId")]
        public virtual BillingPeriod? BillingPeriod { get; set; }

        public virtual ICollection<CallLogStaging> CallLogs { get; set; } = new List<CallLogStaging>();

        // Helper Methods
        public void UpdateStatistics(List<CallLogStaging> logs)
        {
            TotalRecords = logs.Count;
            VerifiedRecords = logs.Count(l => l.VerificationStatus == VerificationStatus.Verified);
            RejectedRecords = logs.Count(l => l.VerificationStatus == VerificationStatus.Rejected);
            PendingRecords = logs.Count(l => l.VerificationStatus == VerificationStatus.Pending);
            RecordsWithAnomalies = logs.Count(l => l.HasAnomalies);
        }

        public string GetStatusBadgeClass()
        {
            return BatchStatus switch
            {
                BatchStatus.Created => "badge-secondary",
                BatchStatus.Processing => "badge-warning",
                BatchStatus.PartiallyVerified => "badge-info",
                BatchStatus.Verified => "badge-success",
                BatchStatus.Published => "badge-primary",
                BatchStatus.Failed => "badge-danger",
                _ => "badge-secondary"
            };
        }

        public string GetStatusDisplayName()
        {
            return BatchStatus switch
            {
                BatchStatus.Created => "Created",
                BatchStatus.Processing => "Processing",
                BatchStatus.PartiallyVerified => "Partially Verified",
                BatchStatus.Verified => "Verified",
                BatchStatus.Published => "Published",
                BatchStatus.Failed => "Failed",
                _ => "Unknown"
            };
        }

        public bool CanBeProcessed()
        {
            return BatchStatus == BatchStatus.Created || BatchStatus == BatchStatus.Failed;
        }

        public bool CanBeVerified()
        {
            return BatchStatus == BatchStatus.Processing || BatchStatus == BatchStatus.PartiallyVerified;
        }

        public bool CanBePublished()
        {
            return BatchStatus == BatchStatus.Verified;
        }
    }

    public enum BatchStatus
    {
        Created,
        Processing,
        PartiallyVerified,
        Verified,
        Published,
        Failed
    }
}