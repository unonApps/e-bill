using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAB.Web.Models
{
    /// <summary>
    /// Email attachments
    /// </summary>
    public class EmailAttachment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Email Log")]
        public int EmailLogId { get; set; }

        [ForeignKey(nameof(EmailLogId))]
        public virtual EmailLog EmailLog { get; set; } = null!;

        [Required]
        [Display(Name = "File Name")]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "File Path")]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Display(Name = "File Size (bytes)")]
        public long FileSize { get; set; }

        [Display(Name = "Content Type")]
        [StringLength(100)]
        public string? ContentType { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
