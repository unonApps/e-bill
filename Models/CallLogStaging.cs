using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAB.Web.Models
{
    public class CallLogStaging
    {
        [Key]
        public int Id { get; set; }

        // Core Call Data
        [MaxLength(50)]
        public string ExtensionNumber { get; set; } = string.Empty;

        public DateTime CallDate { get; set; }

        [MaxLength(50)]
        public string CallNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        public string CallDestination { get; set; } = string.Empty;

        public DateTime CallEndTime { get; set; }

        public int CallDuration { get; set; } // in seconds

        [MaxLength(10)]
        public string CallCurrencyCode { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,4)")]
        public decimal CallCost { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal CallCostUSD { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal CallCostKSHS { get; set; }

        [MaxLength(50)]
        public string CallType { get; set; } = string.Empty; // Voice/SMS/Data

        [MaxLength(50)]
        public string CallDestinationType { get; set; } = string.Empty; // Local/International/Mobile

        // Date Dimensions
        public int CallYear { get; set; }
        public int CallMonth { get; set; }

        // User Mapping
        [MaxLength(50)]
        public string? ResponsibleIndexNumber { get; set; }

        [MaxLength(50)]
        public string? PayingIndexNumber { get; set; }

        // UserPhone relationship - links to specific phone that made the call
        public int? UserPhoneId { get; set; }

        // Billing Period tracking
        public int? BillingPeriodId { get; set; }

        [MaxLength(20)]
        public string ImportType { get; set; } = "MONTHLY"; // MONTHLY or INTERIM

        public bool IsAdjustment { get; set; } = false;

        public int? OriginalRecordId { get; set; } // Links to record being adjusted

        [MaxLength(500)]
        public string? AdjustmentReason { get; set; }

        // Source Information
        [MaxLength(50)]
        public string SourceSystem { get; set; } = string.Empty; // Safaricom/Airtel/PSTN/PrivateWire

        [MaxLength(100)]
        public string? SourceRecordId { get; set; }

        // Staging Metadata
        public Guid BatchId { get; set; }
        public DateTime ImportedDate { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string ImportedBy { get; set; } = string.Empty;

        // Verification Status
        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
        public DateTime? VerificationDate { get; set; }

        [MaxLength(100)]
        public string? VerifiedBy { get; set; }

        public string? VerificationNotes { get; set; }

        // Anomaly Detection
        public bool HasAnomalies { get; set; } = false;

        public string? AnomalyTypes { get; set; } // JSON array of anomaly types

        public string? AnomalyDetails { get; set; } // JSON object with details

        // Processing Status
        public ProcessingStatus ProcessingStatus { get; set; } = ProcessingStatus.Staged;
        public DateTime? ProcessedDate { get; set; }
        public string? ErrorDetails { get; set; }

        // Audit
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedDate { get; set; }

        [MaxLength(100)]
        public string? ModifiedBy { get; set; }

        // Navigation Properties
        [ForeignKey("BatchId")]
        public virtual StagingBatch? Batch { get; set; }

        [ForeignKey("ResponsibleIndexNumber")]
        public virtual EbillUser? ResponsibleUser { get; set; }

        [ForeignKey("PayingIndexNumber")]
        public virtual EbillUser? PayingUser { get; set; }

        [ForeignKey("UserPhoneId")]
        public virtual UserPhone? UserPhone { get; set; }

        [ForeignKey("BillingPeriodId")]
        public virtual BillingPeriod? BillingPeriod { get; set; }

        // Helper methods
        public List<string> GetAnomalyTypesList()
        {
            if (string.IsNullOrEmpty(AnomalyTypes))
                return new List<string>();

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(AnomalyTypes) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        public Dictionary<string, object> GetAnomalyDetailsDictionary()
        {
            if (string.IsNullOrEmpty(AnomalyDetails))
                return new Dictionary<string, object>();

            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(AnomalyDetails) ?? new Dictionary<string, object>();
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }
    }

    public enum VerificationStatus
    {
        Pending,
        Verified,
        Rejected,
        RequiresReview
    }

    public enum ProcessingStatus
    {
        Staged,
        Processing,
        Completed,
        Failed
    }
}