using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAB.Web.Models
{
    public class RefundRequest
    {
        public int Id { get; set; }

        // Mobile Information
        [Required]
        [StringLength(9, MinimumLength = 9)]
        [Display(Name = "Primary Mobile Number")]
        [RegularExpression(@"^\d{9}$", ErrorMessage = "Primary Mobile Number must be exactly 9 digits")]
        public string PrimaryMobileNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Index No")]
        public string IndexNo { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Mobile Number Assigned To")]
        public string MobileNumberAssignedTo { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Office Extension")]
        public string? OfficeExtension { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Office")]
        public string Office { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Mobile Service")]
        public string MobileService { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Class of Service")]
        public string ClassOfService { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Device Allowance must be greater than 0")]
        [Display(Name = "Device Allowance")]
        public decimal DeviceAllowance { get; set; }

        [Display(Name = "Previous Device Reimbursed Date")]
        [DataType(DataType.Date)]
        public DateTime? PreviousDeviceReimbursedDate { get; set; }

        // Purchase Information
        [StringLength(500)]
        [Display(Name = "Purchase Receipt")]
        public string PurchaseReceiptPath { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        [Display(Name = "Device Purchase Currency")]
        public string DevicePurchaseCurrency { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Device Purchase Amount must be greater than 0")]
        [Display(Name = "Device Purchase Amount")]
        public decimal DevicePurchaseAmount { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Organization")]
        public string Organization { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Umoja Bank Name to Use")]
        public string UmojaBankName { get; set; } = string.Empty;

        // Workflow Information
        [Required]
        [StringLength(300)]
        [Display(Name = "Supervisor")]
        public string Supervisor { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Remarks")]
        public string? Remarks { get; set; }

        // System Fields
        [Display(Name = "Request Date")]
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        [StringLength(450)]
        [Display(Name = "Requested By")]
        public string? RequestedBy { get; set; }

        [Display(Name = "Status")]
        public RefundRequestStatus Status { get; set; } = RefundRequestStatus.Draft;

        [Display(Name = "Submitted to Supervisor")]
        public bool SubmittedToSupervisor { get; set; }

        // Supervisor approval fields
        [Display(Name = "Supervisor Approval Date")]
        public DateTime? SupervisorApprovalDate { get; set; }

        [StringLength(500)]
        [Display(Name = "Supervisor Notes")]
        public string? SupervisorNotes { get; set; }

        [StringLength(200)]
        [Display(Name = "Supervisor Remarks")]
        public string? SupervisorRemarks { get; set; }

        [StringLength(300)]
        [Display(Name = "Supervisor Name")]
        public string? SupervisorName { get; set; }

        [StringLength(300)]
        [Display(Name = "Supervisor Email")]
        public string? SupervisorEmail { get; set; }

        // Budget Officer approval fields
        [Display(Name = "Budget Officer Approval Date")]
        public DateTime? BudgetOfficerApprovalDate { get; set; }

        [StringLength(500)]
        [Display(Name = "Budget Officer Notes")]
        public string? BudgetOfficerNotes { get; set; }

        [StringLength(200)]
        [Display(Name = "Budget Officer Remarks")]
        public string? BudgetOfficerRemarks { get; set; }

        [StringLength(300)]
        [Display(Name = "Budget Officer Name")]
        public string? BudgetOfficerName { get; set; }

        [StringLength(300)]
        [Display(Name = "Budget Officer Email")]
        public string? BudgetOfficerEmail { get; set; }

        // Cost Accounting Fields (Budget Officer)
        [StringLength(100)]
        [Display(Name = "Cost Object")]
        public string? CostObject { get; set; }

        [StringLength(100)]
        [Display(Name = "Cost Center")]
        public string? CostCenter { get; set; }

        [StringLength(100)]
        [Display(Name = "Fund Commitment")]
        public string? FundCommitment { get; set; }

        // Staff Claims Unit approval fields
        [Display(Name = "Staff Claims Unit Approval Date")]
        public DateTime? StaffClaimsApprovalDate { get; set; }

        [StringLength(500)]
        [Display(Name = "Staff Claims Unit Notes")]
        public string? StaffClaimsNotes { get; set; }

        [StringLength(200)]
        [Display(Name = "Staff Claims Unit Remarks")]
        public string? StaffClaimsRemarks { get; set; }

        [StringLength(300)]
        [Display(Name = "Staff Claims Unit Officer Name")]
        public string? StaffClaimsOfficerName { get; set; }

        [StringLength(300)]
        [Display(Name = "Staff Claims Unit Officer Email")]
        public string? StaffClaimsOfficerEmail { get; set; }

        // Claims Unit Processing Fields
        [StringLength(100)]
        [Display(Name = "Umoja Payment Document ID")]
        public string? UmojaPaymentDocumentId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Refund USD Amount")]
        public decimal? RefundUsdAmount { get; set; }

        [Display(Name = "Claims Action Date")]
        [DataType(DataType.Date)]
        public DateTime? ClaimsActionDate { get; set; }

        // Payment Approval fields
        [Display(Name = "Payment Approval Date")]
        public DateTime? PaymentApprovalDate { get; set; }

        [StringLength(500)]
        [Display(Name = "Payment Approval Notes")]
        public string? PaymentApprovalNotes { get; set; }

        [StringLength(200)]
        [Display(Name = "Payment Approval Remarks")]
        public string? PaymentApprovalRemarks { get; set; }

        [StringLength(300)]
        [Display(Name = "Payment Approver Name")]
        public string? PaymentApproverName { get; set; }

        [StringLength(300)]
        [Display(Name = "Payment Approver Email")]
        public string? PaymentApproverEmail { get; set; }

        // Cancellation fields
        [Display(Name = "Cancellation Date")]
        public DateTime? CancellationDate { get; set; }

        [StringLength(500)]
        [Display(Name = "Cancellation Reason")]
        public string? CancellationReason { get; set; }

        [StringLength(300)]
        [Display(Name = "Cancelled By")]
        public string? CancelledBy { get; set; }

        // Final completion fields
        [Display(Name = "Completion Date")]
        public DateTime? CompletionDate { get; set; }

        [StringLength(100)]
        [Display(Name = "Payment Reference")]
        public string? PaymentReference { get; set; }

        [StringLength(500)]
        [Display(Name = "Completion Notes")]
        public string? CompletionNotes { get; set; }

        [StringLength(450)]
        [Display(Name = "Processed By")]
        public string? ProcessedBy { get; set; }
    }

    public enum RefundRequestStatus
    {
        [Display(Name = "Draft")]
        Draft = 0,
        [Display(Name = "Pending Supervisor Approval")]
        PendingSupervisor = 1,
        [Display(Name = "Pending Budget Officer")]
        PendingBudgetOfficer = 2,
        [Display(Name = "Pending Staff Claims Unit")]
        PendingStaffClaimsUnit = 3,
        [Display(Name = "Pending Payment Approval")]
        PendingPaymentApproval = 4,
        [Display(Name = "Completed")]
        Completed = 5,
        [Display(Name = "Cancelled")]
        Cancelled = 6
    }
} 