using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAB.Web.Models
{
    /// <summary>
    /// DEPRECATED: This table is no longer actively used.
    /// Legacy call log format has been replaced by specific telecom tables (Safaricom, Airtel, PSTN, PrivateWire).
    /// Kept for historical data compatibility only.
    /// </summary>
    [Obsolete("CallLog table is deprecated. Use Safaricom, Airtel, PSTN, or PrivateWire tables instead.")]
    public class CallLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string AccountNo { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string SubAccountNo { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string SubAccountName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string MSISDN { get; set; } = string.Empty;

        [StringLength(50)]
        public string TaxInvoiceSummaryNo { get; set; } = string.Empty;

        [StringLength(50)]
        public string InvoiceNo { get; set; } = string.Empty;

        [Required]
        public DateTime InvoiceDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetAccessFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetUsageLessTax { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LessTaxes { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? VAT16 { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Excise15 { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal GrossTotal { get; set; }

        // Navigation property to link with EbillUser
        public int? EbillUserId { get; set; }
        public virtual EbillUser? EbillUser { get; set; }

        // Navigation property to link with UserPhone
        public int? UserPhoneId { get; set; }
        public virtual UserPhone? UserPhone { get; set; }

        // Audit fields
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public string? ImportedBy { get; set; }
        public DateTime? ImportedDate { get; set; }
    }
}