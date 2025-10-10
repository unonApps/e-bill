using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAB.Web.Models
{
    public class CallLogReconciliation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BillingPeriodId { get; set; }

        [Required]
        public int SourceRecordId { get; set; }

        [Required]
        [MaxLength(50)]
        public string SourceTable { get; set; } = string.Empty; // Safaricom, Airtel, PSTN, PrivateWire

        // Version tracking
        public int Version { get; set; } = 1;

        [Required]
        [MaxLength(20)]
        public string ImportType { get; set; } = "MONTHLY"; // MONTHLY, INTERIM

        [Required]
        public Guid ImportBatchId { get; set; }

        [Required]
        public DateTime ImportDate { get; set; }

        // Change tracking
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PreviousAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentAmount { get; set; }

        [MaxLength(500)]
        public string? AdjustmentReason { get; set; }

        // Supersession tracking
        public bool IsSuperseded { get; set; } = false;
        public int? SupersededBy { get; set; }
        public DateTime? SupersededDate { get; set; }

        // Navigation properties
        [ForeignKey("BillingPeriodId")]
        public virtual BillingPeriod? BillingPeriod { get; set; }

        [ForeignKey("SupersededBy")]
        public virtual CallLogReconciliation? SupersedingRecord { get; set; }

        // Computed properties
        [NotMapped]
        public decimal AdjustmentAmount => CurrentAmount - (PreviousAmount ?? 0);

        [NotMapped]
        public bool IsAdjustment => PreviousAmount.HasValue && PreviousAmount != CurrentAmount;

        [NotMapped]
        public bool IsOriginal => Version == 1;

        [NotMapped]
        public string ChangeTypeDisplay
        {
            get
            {
                if (!PreviousAmount.HasValue)
                    return "New Record";

                var diff = AdjustmentAmount;
                if (diff == 0)
                    return "No Change";
                else if (diff > 0)
                    return $"Increase (+${Math.Abs(diff):N2})";
                else
                    return $"Decrease (-${Math.Abs(diff):N2})";
            }
        }

        [NotMapped]
        public string VersionDisplay => Version == 1 ? "Original" : $"Version {Version}";

        // Methods
        public CallLogReconciliation CreateNewVersion(decimal newAmount, string reason, Guid batchId)
        {
            // Mark current version as superseded
            this.IsSuperseded = true;
            this.SupersededDate = DateTime.UtcNow;

            // Create new version
            var newVersion = new CallLogReconciliation
            {
                BillingPeriodId = this.BillingPeriodId,
                SourceRecordId = this.SourceRecordId,
                SourceTable = this.SourceTable,
                Version = this.Version + 1,
                ImportType = "INTERIM",
                ImportBatchId = batchId,
                ImportDate = DateTime.UtcNow,
                PreviousAmount = this.CurrentAmount,
                CurrentAmount = newAmount,
                AdjustmentReason = reason
            };

            // Link the records
            this.SupersededBy = newVersion.Id;

            return newVersion;
        }

        public override string ToString()
        {
            return $"{SourceTable}#{SourceRecordId} v{Version} - ${CurrentAmount:N2}";
        }
    }

    // Summary class for reconciliation reports
    public class ReconciliationSummary
    {
        public int BillingPeriodId { get; set; }
        public string PeriodCode { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
        public int AdjustedRecords { get; set; }
        public int NewRecords { get; set; }
        public decimal OriginalAmount { get; set; }
        public decimal AdjustmentAmount { get; set; }
        public decimal FinalAmount { get; set; }

        public Dictionary<string, int> RecordsBySource { get; set; } = new();
        public Dictionary<string, decimal> AmountsBySource { get; set; } = new();
        public List<ReconciliationDetail> Details { get; set; } = new();
    }

    public class ReconciliationDetail
    {
        public string SourceTable { get; set; } = string.Empty;
        public int SourceRecordId { get; set; }
        public int VersionCount { get; set; }
        public decimal OriginalAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public decimal TotalAdjustment { get; set; }
        public string LastReason { get; set; } = string.Empty;
        public DateTime LastModified { get; set; }
    }
}