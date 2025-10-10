using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAB.Web.Models
{
    /// <summary>
    /// Airtel Call Data Record
    /// Represents telecommunications records from Airtel service provider
    /// </summary>
    [Table("Airtel")]
    public class Airtel
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(50)]
        [Display(Name = "Extension")]
        public string? Ext { get; set; }

        [Display(Name = "Call Date")]
        [Column("call_date")]
        public DateTime? CallDate { get; set; }

        [Display(Name = "Call Time")]
        [Column("call_time")]
        public TimeSpan? CallTime { get; set; }

        [MaxLength(100)]
        [Display(Name = "Dialed Number")]
        public string? Dialed { get; set; }

        [MaxLength(200)]
        [Display(Name = "Destination")]
        public string? Dest { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Duration Extended")]
        public decimal? Durx { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Cost (KES)")]
        public decimal? Cost { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Cost (USD)")]
        public decimal? AmountUSD { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Duration")]
        public decimal? Dur { get; set; }

        [MaxLength(50)]
        [Display(Name = "Call Type")]
        [Column("call_type")]
        public string? CallType { get; set; }

        [Display(Name = "Call Month")]
        [Column("call_month")]
        public int? CallMonth { get; set; }

        [Display(Name = "Call Year")]
        [Column("call_year")]
        public int? CallYear { get; set; }

        // Source column removed - redundant as table name indicates source

        // Organization hierarchy obtained through EbillUserId relationship

        [MaxLength(50)]
        [Display(Name = "Index Number")]
        public string? IndexNumber { get; set; }

        // User Phone Relationship - links call to registered phone
        [Display(Name = "User Phone ID")]
        public int? UserPhoneId { get; set; }

        [ForeignKey("UserPhoneId")]
        public virtual UserPhone? UserPhone { get; set; }

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
        public string FormattedDuration => Dur.HasValue
            ? $"{(int)Dur.Value}:{((Dur.Value % 1) * 60):00}"
            : "0:00";

        [NotMapped]
        public string FormattedDurationExtended => Durx.HasValue
            ? $"{(int)Durx.Value}:{((Durx.Value % 1) * 60):00}"
            : "0:00";

        [NotMapped]
        public bool IsInternational => Dialed?.StartsWith("+") ?? false;

        [NotMapped]
        public bool IsLocal => CallType?.ToLower().Contains("local") ?? false;

        [NotMapped]
        public string ServiceProvider => "Airtel";

        [NotMapped]
        public string MonthYearDisplay => CallMonth.HasValue && CallYear.HasValue
            ? $"{CallMonth:00}/{CallYear}"
            : "";
    }
}