using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAB.Web.Models
{
    /// <summary>
    /// Documents supporting the phone overage justification
    /// Examples: approval emails, memos, official letters, etc.
    /// </summary>
    public class PhoneOverageDocument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PhoneOverageJustificationId { get; set; }

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

        [MaxLength(500)]
        public string? Description { get; set; }

        // Upload Details
        [Required]
        [MaxLength(50)]
        public string UploadedBy { get; set; } = string.Empty;

        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("PhoneOverageJustificationId")]
        public virtual PhoneOverageJustification PhoneOverageJustification { get; set; } = null!;
    }
}
