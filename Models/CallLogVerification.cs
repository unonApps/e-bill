using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TAB.Web.Models.Enums;

namespace TAB.Web.Models
{
    public class CallLogVerification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CallRecordId { get; set; }

        [Required]
        [MaxLength(50)]
        public string VerifiedBy { get; set; } = string.Empty;

        [Required]
        public DateTime VerifiedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public VerificationType VerificationType { get; set; }

        // Class of Service Tracking
        public int? ClassOfServiceId { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? AllowanceAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,4)")]
        public decimal ActualAmount { get; set; }

        public bool IsOverage { get; set; } = false;

        [Column(TypeName = "decimal(18,4)")]
        public decimal OverageAmount { get; set; } = 0;

        public bool OverageJustified { get; set; } = false;

        // Overage Justification
        public string? JustificationText { get; set; }

        public string? SupportingDocuments { get; set; } // JSON

        // Payment Assignment
        public int? PaymentAssignmentId { get; set; }

        // Approval Workflow
        [Required]
        [MaxLength(20)]
        public string ApprovalStatus { get; set; } = "Pending";

        public bool SubmittedToSupervisor { get; set; } = false;

        public DateTime? SubmittedDate { get; set; }

        [MaxLength(50)]
        public string? SupervisorIndexNumber { get; set; }

        [MaxLength(20)]
        public string? SupervisorApprovalStatus { get; set; }

        [MaxLength(50)]
        public string? SupervisorApprovedBy { get; set; }

        public DateTime? SupervisorApprovedDate { get; set; }

        [MaxLength(500)]
        public string? SupervisorComments { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? ApprovedAmount { get; set; }

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        // Audit
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        // Navigation Properties
        [ForeignKey("CallRecordId")]
        public virtual CallRecord CallRecord { get; set; } = null!;

        [ForeignKey("ClassOfServiceId")]
        public virtual ClassOfService? ClassOfService { get; set; }

        [ForeignKey("PaymentAssignmentId")]
        public virtual CallLogPaymentAssignment? PaymentAssignment { get; set; }

        public virtual ICollection<CallLogDocument> Documents { get; set; } = new List<CallLogDocument>();

        // Helper Properties
        [NotMapped]
        public bool IsPending => ApprovalStatus == "Pending";

        [NotMapped]
        public bool IsApproved => ApprovalStatus == "Approved" || ApprovalStatus == "PartiallyApproved";
    }
}
