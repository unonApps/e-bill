using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAB.Web.Models
{
    /// <summary>
    /// PSTN (Public Switched Telephone Network) Call Record
    /// Represents telephone call logs from traditional phone systems
    /// </summary>
    public class PSTN
    {
        [Key]
        public int Id { get; set; }

        // Call Origin Information
        [MaxLength(50)]
        [Display(Name = "Extension")]
        public string? Extension { get; set; } // Was: Ext

        [MaxLength(100)]
        [Display(Name = "Dialed Number")]
        public string? DialedNumber { get; set; } // Was: Dialed

        [Display(Name = "Call Time")]
        public TimeSpan? CallTime { get; set; } // Was: Time

        // Call Destination Information
        [MaxLength(200)]
        [Display(Name = "Destination")]
        public string? Destination { get; set; } // Was: Dest

        [MaxLength(50)]
        [Display(Name = "Destination Line")]
        public string? DestinationLine { get; set; } // Was: Dl

        // Duration Information
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Duration Extended (minutes)")]
        public decimal? DurationExtended { get; set; } // Was: Durx

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Duration (minutes)")]
        public decimal? Duration { get; set; } // Was: Dur

        // Organization hierarchy obtained through EbillUserId relationship

        // Removed redundant column - use EbillUser relationship instead
        // OrganizationalUnit - get from EbillUser.Organization

        // Caller Information

        [Display(Name = "Call Date")]
        public DateTime? CallDate { get; set; } // Was: Date

        [Display(Name = "Call Month")]
        public int CallMonth { get; set; }

        [Display(Name = "Call Year")]
        public int CallYear { get; set; }

        // Billing Information
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Amount (KES)")]
        public decimal? AmountKSH { get; set; } // Was: Kshs

        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Amount (USD)")]
        public decimal? AmountUSD { get; set; }

        [MaxLength(50)]
        [Display(Name = "Index Number")]
        public string? IndexNumber { get; set; } // Was: Inde_

        // User Phone Relationship - links call to registered phone
        [Display(Name = "User Phone ID")]
        public int? UserPhoneId { get; set; }

        [ForeignKey("UserPhoneId")]
        public virtual UserPhone? UserPhone { get; set; }

        // Removed redundant columns - use EbillUser relationship instead
        // Location, OCACode - get from EbillUser.Location

        // Additional Fields

        [MaxLength(50)]
        [Display(Name = "Carrier")]
        public string? Carrier { get; set; } // Was: Car

        // Audit fields
        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        [Display(Name = "Created By")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Modified Date")]
        public DateTime? ModifiedDate { get; set; }

        [MaxLength(100)]
        [Display(Name = "Modified By")]
        public string? ModifiedBy { get; set; }

        // User relationship
        [Display(Name = "E-bill User ID")]
        public int? EbillUserId { get; set; }

        // Navigation property
        public virtual EbillUser? EbillUser { get; set; }

        // Import tracking
        [Display(Name = "Import Audit ID")]
        public int? ImportAuditId { get; set; }

        public virtual ImportAudit? ImportAudit { get; set; }

        // Processing status for cleanup tracking
        [Display(Name = "Processing Status")]
        public ProcessingStatus ProcessingStatus { get; set; } = ProcessingStatus.Staged;

        [Display(Name = "Processed Date")]
        public DateTime? ProcessedDate { get; set; }

        // Staging tracking - links to CallLogStaging batch
        [Display(Name = "Staging Batch ID")]
        public Guid? StagingBatchId { get; set; }

        // Billing period for monthly reconciliation
        [MaxLength(20)]
        [Display(Name = "Billing Period")]
        public string? BillingPeriod { get; set; }

        // Calculated properties for reporting
        [NotMapped]
        public decimal TotalCost => AmountKSH ?? 0;

        [NotMapped]
        public string FormattedDuration => Duration.HasValue
            ? $"{(int)Duration.Value}:{((Duration.Value % 1) * 60):00}"
            : "0:00";

        [NotMapped]
        public bool IsInternational => DialedNumber?.StartsWith("+") ?? false;
    }
}