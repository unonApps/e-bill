using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TAB.Web.Models.Enums;

namespace TAB.Web.Models
{
    public class CallLogDocument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CallLogVerificationId { get; set; }

        // File Details
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        public long FileSize { get; set; }

        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; } = string.Empty;

        // Document Type
        [Required]
        [MaxLength(50)]
        public string DocumentType { get; set; } = string.Empty;
        // Values: OverageJustification, ApprovalLetter, Receipt, Other

        [MaxLength(500)]
        public string? Description { get; set; }

        // Upload Details
        [Required]
        [MaxLength(50)]
        public string UploadedBy { get; set; } = string.Empty;

        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("CallLogVerificationId")]
        public virtual CallLogVerification CallLogVerification { get; set; } = null!;
    }
}
