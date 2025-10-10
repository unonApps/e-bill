using System.ComponentModel.DataAnnotations;

namespace TAB.Web.Models
{
    public class Ebill
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(300)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Department")]
        public string Department { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Service Provider")]
        public int ServiceProviderId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Account Number")]
        public string AccountNumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Bill Month")]
        [DataType(DataType.Date)]
        public DateTime BillMonth { get; set; }

        [Required]
        [Display(Name = "Bill Amount")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Bill amount must be greater than 0")]
        public decimal BillAmount { get; set; }

        [Required]
        [Display(Name = "Due Date")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Required]
        [Display(Name = "Bill Type")]
        public BillType BillType { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [StringLength(500)]
        [Display(Name = "Additional Notes")]
        public string? AdditionalNotes { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Supervisor")]
        public string Supervisor { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Status")]
        public EbillStatus Status { get; set; } = EbillStatus.Draft;

        [Display(Name = "Request Date")]
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Processed Date")]
        public DateTime? ProcessedDate { get; set; }

        [StringLength(450)]
        [Display(Name = "Requested By")]
        public string RequestedBy { get; set; } = string.Empty;

        [StringLength(450)]
        [Display(Name = "Processed By")]
        public string? ProcessedBy { get; set; }

        [StringLength(500)]
        [Display(Name = "Processing Notes")]
        public string? ProcessingNotes { get; set; }

        [Display(Name = "Payment Date")]
        [DataType(DataType.Date)]
        public DateTime? PaymentDate { get; set; }

        [Display(Name = "Paid Amount")]
        public decimal? PaidAmount { get; set; }

        // Supervisor approval fields
        [Display(Name = "Submitted to Supervisor")]
        public bool SubmittedToSupervisor { get; set; } = false;

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

        // Navigation properties
        public virtual Models.ServiceProvider? ServiceProvider { get; set; }
    }

    public enum BillType
    {
        [Display(Name = "Mobile Bill")]
        Mobile = 1,
        [Display(Name = "Internet Bill")]
        Internet = 2,
        [Display(Name = "Landline Bill")]
        Landline = 3,
        [Display(Name = "Data Bill")]
        Data = 4,
        [Display(Name = "Other")]
        Other = 5
    }

    public enum EbillStatus
    {
        [Display(Name = "Draft")]
        Draft = 0,
        [Display(Name = "Pending Supervisor Approval")]
        PendingSupervisor = 1,
        [Display(Name = "Pending Admin Approval")]
        PendingAdmin = 2,
        [Display(Name = "Approved")]
        Approved = 3,
        [Display(Name = "Rejected")]
        Rejected = 4,
        [Display(Name = "Paid")]
        Paid = 5,
        [Display(Name = "Overdue")]
        Overdue = 6
    }
} 