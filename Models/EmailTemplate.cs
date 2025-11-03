using System.ComponentModel.DataAnnotations;

namespace TAB.Web.Models
{
    /// <summary>
    /// Email template with placeholders for dynamic content
    /// </summary>
    public class EmailTemplate
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Template name is required")]
        [Display(Name = "Template Name")]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Template code is required")]
        [Display(Name = "Template Code")]
        [StringLength(100)]
        public string TemplateCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject is required")]
        [Display(Name = "Email Subject")]
        [StringLength(500)]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "HTML body is required")]
        [Display(Name = "HTML Body")]
        public string HtmlBody { get; set; } = string.Empty;

        [Display(Name = "Plain Text Body")]
        public string? PlainTextBody { get; set; }

        [Display(Name = "Description")]
        [StringLength(1000)]
        public string? Description { get; set; }

        [Display(Name = "Available Placeholders")]
        [StringLength(2000)]
        public string? AvailablePlaceholders { get; set; }

        [Display(Name = "Category")]
        [StringLength(100)]
        public string? Category { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Is System Template")]
        public bool IsSystemTemplate { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        [StringLength(100)]
        public string? ModifiedBy { get; set; }

        // Navigation property
        public virtual ICollection<EmailLog> EmailLogs { get; set; } = new List<EmailLog>();
    }
}
