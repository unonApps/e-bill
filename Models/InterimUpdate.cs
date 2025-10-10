using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace TAB.Web.Models
{
    public class InterimUpdate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BillingPeriodId { get; set; }

        [Required]
        [MaxLength(50)]
        public string UpdateType { get; set; } = string.Empty; // CORRECTION, DISPUTE, LATE_ARRIVAL, ADJUSTMENT

        [Required]
        public Guid BatchId { get; set; }

        // Change tracking
        public int RecordsAdded { get; set; }
        public int RecordsModified { get; set; }
        public int RecordsDeleted { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal NetAdjustmentAmount { get; set; }

        // Approval workflow
        [Required]
        [MaxLength(100)]
        public string RequestedBy { get; set; } = string.Empty;

        [Required]
        public DateTime RequestedDate { get; set; }

        [MaxLength(100)]
        public string? ApprovedBy { get; set; }

        public DateTime? ApprovalDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string ApprovalStatus { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        // Documentation
        [Required]
        public string Justification { get; set; } = string.Empty;

        public string? SupportingDocuments { get; set; } // JSON array of file paths

        // Processing
        public DateTime? ProcessedDate { get; set; }
        public string? ProcessingNotes { get; set; }

        // Navigation properties
        [ForeignKey("BillingPeriodId")]
        public virtual BillingPeriod? BillingPeriod { get; set; }

        [ForeignKey("BatchId")]
        public virtual StagingBatch? Batch { get; set; }

        // Computed properties
        [NotMapped]
        public int TotalRecordsAffected => RecordsAdded + RecordsModified + RecordsDeleted;

        [NotMapped]
        public bool IsPending => ApprovalStatus == "PENDING";

        [NotMapped]
        public bool IsApproved => ApprovalStatus == "APPROVED";

        [NotMapped]
        public bool IsRejected => ApprovalStatus == "REJECTED";

        [NotMapped]
        public bool IsProcessed => ProcessedDate.HasValue;

        [NotMapped]
        public string UpdateTypeDisplay
        {
            get
            {
                return UpdateType switch
                {
                    "CORRECTION" => "Billing Correction",
                    "DISPUTE" => "Disputed Charges",
                    "LATE_ARRIVAL" => "Late Arriving Bills",
                    "ADJUSTMENT" => "Manual Adjustment",
                    _ => UpdateType
                };
            }
        }

        [NotMapped]
        public string UpdateTypeIcon
        {
            get
            {
                return UpdateType switch
                {
                    "CORRECTION" => "🔧",
                    "DISPUTE" => "⚠️",
                    "LATE_ARRIVAL" => "⏰",
                    "ADJUSTMENT" => "✏️",
                    _ => "📝"
                };
            }
        }

        [NotMapped]
        public string ApprovalStatusBadgeClass
        {
            get
            {
                return ApprovalStatus switch
                {
                    "PENDING" => "badge-warning",
                    "APPROVED" => "badge-success",
                    "REJECTED" => "badge-danger",
                    _ => "badge-secondary"
                };
            }
        }

        [NotMapped]
        public List<string> DocumentPaths
        {
            get
            {
                if (string.IsNullOrEmpty(SupportingDocuments))
                    return new List<string>();

                try
                {
                    return JsonSerializer.Deserialize<List<string>>(SupportingDocuments) ?? new List<string>();
                }
                catch
                {
                    return new List<string>();
                }
            }
        }

        // Methods
        public void AddDocument(string filePath)
        {
            var docs = DocumentPaths;
            docs.Add(filePath);
            SupportingDocuments = JsonSerializer.Serialize(docs);
        }

        public void Approve(string approvedBy)
        {
            if (!IsPending)
                throw new InvalidOperationException("Only pending updates can be approved");

            ApprovalStatus = "APPROVED";
            ApprovedBy = approvedBy;
            ApprovalDate = DateTime.UtcNow;
        }

        public void Reject(string rejectedBy, string reason)
        {
            if (!IsPending)
                throw new InvalidOperationException("Only pending updates can be rejected");

            ApprovalStatus = "REJECTED";
            ApprovedBy = rejectedBy;
            ApprovalDate = DateTime.UtcNow;
            RejectionReason = reason;
        }

        public void MarkProcessed(string notes = null)
        {
            if (!IsApproved)
                throw new InvalidOperationException("Only approved updates can be processed");

            ProcessedDate = DateTime.UtcNow;
            ProcessingNotes = notes;
        }

        public override string ToString()
        {
            return $"{UpdateTypeDisplay} - {BillingPeriod?.PeriodCode} ({ApprovalStatus})";
        }
    }

    // Enum-like class for update types
    public static class InterimUpdateType
    {
        public const string Correction = "CORRECTION";
        public const string Dispute = "DISPUTE";
        public const string LateArrival = "LATE_ARRIVAL";
        public const string Adjustment = "ADJUSTMENT";

        public static readonly List<string> AllTypes = new()
        {
            Correction, Dispute, LateArrival, Adjustment
        };

        public static bool IsValidType(string type)
        {
            return AllTypes.Contains(type);
        }
    }

    // Request model for creating interim updates
    public class InterimUpdateRequest
    {
        public int BillingPeriodId { get; set; }
        public string UpdateType { get; set; } = InterimUpdateType.Correction;
        public string Justification { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = string.Empty;
        public List<IFormFile>? SupportingFiles { get; set; }
    }
}