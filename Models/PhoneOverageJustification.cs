using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAB.Web.Models
{
    /// <summary>
    /// Stores overage justification at the phone/extension level for a specific month
    /// This replaces per-call justification - staff provides ONE justification per phone per month
    /// </summary>
    public class PhoneOverageJustification
    {
        [Key]
        public int Id { get; set; }

        // Which phone/extension has overage
        [Required]
        public int UserPhoneId { get; set; }

        // Which month/year
        [Required]
        public int Month { get; set; }

        [Required]
        public int Year { get; set; }

        // Overage details
        [Column(TypeName = "decimal(18,4)")]
        public decimal AllowanceLimit { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalUsage { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal OverageAmount { get; set; }

        // Justification
        [Required]
        public string JustificationText { get; set; } = string.Empty;

        // Submission tracking
        [Required]
        [MaxLength(50)]
        public string SubmittedBy { get; set; } = string.Empty;

        public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;

        // Approval tracking
        [MaxLength(20)]
        public string? ApprovalStatus { get; set; } = "Pending"; // Pending, Approved, Rejected

        [MaxLength(50)]
        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedDate { get; set; }

        [MaxLength(500)]
        public string? ApprovalComments { get; set; }

        // Navigation Properties
        [ForeignKey("UserPhoneId")]
        public virtual UserPhone UserPhone { get; set; } = null!;

        public virtual ICollection<PhoneOverageDocument> Documents { get; set; } = new List<PhoneOverageDocument>();
    }
}
