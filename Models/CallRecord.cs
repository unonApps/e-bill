using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TAB.Web.Models.Enums;

namespace TAB.Web.Models
{
    public class CallRecord
    {
        [Key]
        public int Id { get; set; }

        // Core Call Data (matching your existing calls_data structure)
        [MaxLength(50)]
        [Column("ext_no")]
        public string ExtensionNumber { get; set; } = string.Empty;

        [Column("call_date")]
        public DateTime CallDate { get; set; }

        [MaxLength(50)]
        [Column("call_number")]
        public string CallNumber { get; set; } = string.Empty;

        [MaxLength(100)]
        [Column("call_destination")]
        public string CallDestination { get; set; } = string.Empty;

        [Column("call_endtime")]
        public DateTime CallEndTime { get; set; }

        [Column("call_duration")]
        public int CallDuration { get; set; } // in seconds

        [MaxLength(10)]
        [Column("call_curr_code")]
        public string CallCurrencyCode { get; set; } = string.Empty;

        [Column("call_cost", TypeName = "decimal(18,4)")]
        public decimal CallCost { get; set; }

        [Column("call_cost_usd", TypeName = "decimal(18,4)")]
        public decimal CallCostUSD { get; set; }

        [Column("call_cost_kshs", TypeName = "decimal(18,4)")]
        public decimal CallCostKSHS { get; set; }

        [MaxLength(50)]
        [Column("call_type")]
        public string CallType { get; set; } = string.Empty;

        [MaxLength(50)]
        [Column("call_dest_type")]
        public string CallDestinationType { get; set; } = string.Empty;

        [Column("call_year")]
        public int CallYear { get; set; }

        [Column("call_month")]
        public int CallMonth { get; set; }

        // User responsibility and payment
        [MaxLength(50)]
        [Column("ext_resp_index")]
        public string? ResponsibleIndexNumber { get; set; }

        [MaxLength(50)]
        [Column("call_pay_index")]
        public string? PayingIndexNumber { get; set; }

        // Verification indicators
        [Column("call_ver_ind")]
        public bool IsVerified { get; set; } = false;

        [Column("call_ver_date")]
        public DateTime? VerificationDate { get; set; }

        // Verification period (deadline for verification)
        [Column("verification_period")]
        public DateTime? VerificationPeriod { get; set; }

        // Approval period (deadline for supervisor approval)
        [Column("approval_period")]
        public DateTime? ApprovalPeriod { get; set; }

        // Revert tracking
        [Column("revert_count")]
        public int RevertCount { get; set; } = 0;

        [Column("last_revert_date")]
        public DateTime? LastRevertDate { get; set; }

        [Column("revert_reason")]
        [MaxLength(500)]
        public string? RevertReason { get; set; }

        // Verification Type (Personal/Official)
        [MaxLength(20)]
        [Column("verification_type")]
        public string? VerificationType { get; set; }

        // Payment Assignment
        [Column("payment_assignment_id")]
        public int? PaymentAssignmentId { get; set; }

        // Assignment Status
        [Column("assignment_status")]
        [MaxLength(20)]
        public string AssignmentStatus { get; set; } = "None";

        [Column("overage_justified")]
        public bool OverageJustified { get; set; } = false;

        // Supervisor Approval
        [MaxLength(20)]
        [Column("supervisor_approval_status")]
        public string? SupervisorApprovalStatus { get; set; }

        [MaxLength(50)]
        [Column("supervisor_approved_by")]
        public string? SupervisorApprovedBy { get; set; }

        [Column("supervisor_approved_date")]
        public DateTime? SupervisorApprovedDate { get; set; }

        // Recovery Tracking
        [MaxLength(50)]
        [Column("recovery_status")]
        public string? RecoveryStatus { get; set; } = "NotProcessed";

        [Column("recovery_date")]
        public DateTime? RecoveryDate { get; set; }

        [MaxLength(100)]
        [Column("recovery_processed_by")]
        public string? RecoveryProcessedBy { get; set; }

        [MaxLength(50)]
        [Column("final_assignment_type")]
        public string? FinalAssignmentType { get; set; }

        [Column("recovery_amount", TypeName = "decimal(18,2)")]
        public decimal? RecoveryAmount { get; set; }

        // Certification indicators
        [Column("call_cert_ind")]
        public bool IsCertified { get; set; } = false;

        [Column("call_cert_date")]
        public DateTime? CertificationDate { get; set; }

        [MaxLength(100)]
        [Column("call_cert_by")]
        public string? CertifiedBy { get; set; }

        // Processing indicator
        [Column("call_proc_ind")]
        public bool IsProcessed { get; set; } = false;

        [Column("entry_date")]
        public DateTime EntryDate { get; set; } = DateTime.UtcNow;

        [MaxLength(200)]
        [Column("call_dest_descr")]
        public string? CallDestinationDescription { get; set; }

        // Additional metadata for tracking
        [MaxLength(50)]
        public string? SourceSystem { get; set; }

        public Guid? SourceBatchId { get; set; }

        public int? SourceStagingId { get; set; }

        // UserPhone ID for foreign key relationship
        public int? UserPhoneId { get; set; }

        // Navigation Properties
        [ForeignKey("UserPhoneId")]
        public virtual UserPhone? UserPhone { get; set; }

        [ForeignKey("ResponsibleIndexNumber")]
        public virtual EbillUser? ResponsibleUser { get; set; }

        [ForeignKey("PayingIndexNumber")]
        public virtual EbillUser? PayingUser { get; set; }

        // Computed Properties
        [NotMapped]
        public decimal Amount => CallCostUSD;

        [NotMapped]
        public string IndexNumber => ResponsibleIndexNumber ?? string.Empty;

        [NotMapped]
        public string PhoneNumber => ExtensionNumber;

        // Helper Methods
        public string GetCallTypeIcon()
        {
            return CallType?.ToLower() switch
            {
                "voice" => "bi-telephone-fill",
                "sms" => "bi-chat-dots-fill",
                "data" => "bi-wifi",
                _ => "bi-phone"
            };
        }

        public string GetDestinationTypeColor()
        {
            return CallDestinationType?.ToLower() switch
            {
                "local" => "success",
                "international" => "danger",
                "mobile" => "info",
                "premium" => "warning",
                _ => "secondary"
            };
        }

        public string GetDurationFormatted()
        {
            var hours = CallDuration / 3600;
            var minutes = (CallDuration % 3600) / 60;
            var seconds = CallDuration % 60;

            if (hours > 0)
                return $"{hours}h {minutes}m {seconds}s";
            else if (minutes > 0)
                return $"{minutes}m {seconds}s";
            else
                return $"{seconds}s";
        }

        public bool IsHighCost(decimal threshold = 100)
        {
            return CallCostUSD > threshold;
        }

        public bool IsLongDuration(int thresholdMinutes = 60)
        {
            return CallDuration > (thresholdMinutes * 60);
        }
    }
}