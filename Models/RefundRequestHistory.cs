using System.ComponentModel.DataAnnotations;

namespace TAB.Web.Models
{
    public class RefundRequestHistory
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Refund Request ID")]
        public int RefundRequestId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Action")]
        public string Action { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Previous Status")]
        public string? PreviousStatus { get; set; }

        [StringLength(50)]
        [Display(Name = "New Status")]
        public string? NewStatus { get; set; }

        [StringLength(1000)]
        [Display(Name = "Comments")]
        public string? Comments { get; set; }

        [Required]
        [StringLength(450)]
        [Display(Name = "Performed By")]
        public string PerformedBy { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "User Name")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        [Display(Name = "IP Address")]
        public string? IpAddress { get; set; }

        // Navigation property
        public virtual RefundRequest? RefundRequest { get; set; }
    }

    public static class RefundHistoryActions
    {
        public const string Created = "Created";
        public const string Updated = "Updated";
        public const string SubmittedToSupervisor = "Submitted to Supervisor";
        public const string SupervisorApproved = "Supervisor Approved";
        public const string SupervisorRejected = "Supervisor Rejected";
        public const string SupervisorReverted = "Supervisor Reverted to Requestor";
        public const string BudgetOfficerApproved = "Budget Officer Approved";
        public const string BudgetOfficerRejected = "Budget Officer Rejected";
        public const string BudgetOfficerReverted = "Budget Officer Reverted to Requestor";
        public const string BudgetOfficerReassigned = "Budget Officer Reassigned";
        public const string ClaimsUnitProcessed = "Claims Unit Processed";
        public const string ClaimsUnitRejected = "Claims Unit Rejected";
        public const string ClaimsUnitRevertedToRequestor = "Claims Unit Reverted to Requestor";
        public const string ClaimsUnitRevertedToBudgetOfficer = "Claims Unit Reverted to Budget Officer";
        public const string PaymentApproved = "Payment Approved";
        public const string PaymentRejected = "Payment Rejected";
        public const string PaymentRevertedToClaims = "Payment Reverted to Claims Unit";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";
    }
}
