using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAB.Web.Models
{
    public class BillingPeriod
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string PeriodCode { get; set; } = string.Empty; // Format: "2024-09"

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "OPEN"; // OPEN, PROCESSING, CLOSED, LOCKED

        // Monthly billing information
        public DateTime? MonthlyImportDate { get; set; }
        public Guid? MonthlyBatchId { get; set; }
        public int MonthlyRecordCount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyTotalCost { get; set; }

        // Interim updates tracking
        public int InterimUpdateCount { get; set; }
        public DateTime? LastInterimDate { get; set; }
        public int InterimRecordCount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal InterimAdjustmentAmount { get; set; }

        // Closure information
        public DateTime? ClosedDate { get; set; }

        [MaxLength(100)]
        public string? ClosedBy { get; set; }

        public DateTime? LockedDate { get; set; }

        [MaxLength(100)]
        public string? LockedBy { get; set; }

        // Audit fields
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string CreatedBy { get; set; } = "System";

        public DateTime? ModifiedDate { get; set; }

        [MaxLength(100)]
        public string? ModifiedBy { get; set; }

        public string? Notes { get; set; }

        // Navigation properties
        public virtual ICollection<InterimUpdate> InterimUpdates { get; set; } = new List<InterimUpdate>();
        public virtual ICollection<CallLogReconciliation> Reconciliations { get; set; } = new List<CallLogReconciliation>();
        public virtual ICollection<CallLogStaging> StagingRecords { get; set; } = new List<CallLogStaging>();
        public virtual ICollection<StagingBatch> StagingBatches { get; set; } = new List<StagingBatch>();

        // Computed properties
        [NotMapped]
        public decimal TotalAmount => MonthlyTotalCost + InterimAdjustmentAmount;

        [NotMapped]
        public bool IsOpen => Status == "OPEN";

        [NotMapped]
        public bool IsClosed => Status == "CLOSED" || Status == "LOCKED";

        [NotMapped]
        public bool CanAcceptInterim => Status == "OPEN" || Status == "PROCESSING";

        [NotMapped]
        public string StatusBadgeClass
        {
            get
            {
                return Status switch
                {
                    "OPEN" => "badge-success",
                    "PROCESSING" => "badge-warning",
                    "CLOSED" => "badge-info",
                    "LOCKED" => "badge-dark",
                    _ => "badge-secondary"
                };
            }
        }

        [NotMapped]
        public string StatusIcon
        {
            get
            {
                return Status switch
                {
                    "OPEN" => "📂",
                    "PROCESSING" => "⚙️",
                    "CLOSED" => "✅",
                    "LOCKED" => "🔒",
                    _ => "❓"
                };
            }
        }

        // Methods
        public bool CanImportMonthly()
        {
            return Status == "OPEN" && MonthlyBatchId == null;
        }

        public bool CanClose()
        {
            return Status == "PROCESSING" || (Status == "OPEN" && MonthlyBatchId.HasValue);
        }

        public bool RequiresApprovalForChanges()
        {
            return Status == "CLOSED" || Status == "LOCKED";
        }

        public void UpdateStatistics(List<CallLogStaging> stagingRecords)
        {
            if (stagingRecords == null || !stagingRecords.Any())
                return;

            var monthlyRecords = stagingRecords.Where(s => s.ImportType == "MONTHLY").ToList();
            var interimRecords = stagingRecords.Where(s => s.ImportType == "INTERIM").ToList();

            if (monthlyRecords.Any())
            {
                MonthlyRecordCount = monthlyRecords.Count;
                MonthlyTotalCost = monthlyRecords.Sum(r => r.CallCostUSD);
            }

            if (interimRecords.Any())
            {
                InterimRecordCount += interimRecords.Count;
                InterimAdjustmentAmount += interimRecords.Sum(r => r.CallCostUSD);
                LastInterimDate = DateTime.UtcNow;
            }

            ModifiedDate = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return $"{PeriodCode} ({Status}) - Total: ${TotalAmount:N2}";
        }
    }

    // Enum for period status
    public static class BillingPeriodStatus
    {
        public const string Open = "OPEN";
        public const string Processing = "PROCESSING";
        public const string Closed = "CLOSED";
        public const string Locked = "LOCKED";

        public static readonly List<string> AllStatuses = new()
        {
            Open, Processing, Closed, Locked
        };

        public static bool IsValidStatus(string status)
        {
            return AllStatuses.Contains(status);
        }
    }
}