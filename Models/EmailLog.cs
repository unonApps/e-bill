using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAB.Web.Models
{
    /// <summary>
    /// Log of all emails sent from the system
    /// </summary>
    public class EmailLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "To Email")]
        [StringLength(255)]
        public string ToEmail { get; set; } = string.Empty;

        [Display(Name = "CC Emails")]
        [StringLength(1000)]
        public string? CcEmails { get; set; }

        [Display(Name = "BCC Emails")]
        [StringLength(1000)]
        public string? BccEmails { get; set; }

        [Required]
        [Display(Name = "Subject")]
        [StringLength(500)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Email Body")]
        public string Body { get; set; } = string.Empty;

        [Display(Name = "Plain Text Body")]
        public string? PlainTextBody { get; set; }

        [Display(Name = "Template Used")]
        public int? EmailTemplateId { get; set; }

        [ForeignKey(nameof(EmailTemplateId))]
        public virtual EmailTemplate? EmailTemplate { get; set; }

        [Display(Name = "Status")]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Sent, Failed, Queued

        [Display(Name = "Sent Date")]
        public DateTime? SentDate { get; set; }

        [Display(Name = "Error Message")]
        [StringLength(2000)]
        public string? ErrorMessage { get; set; }

        [Display(Name = "Retry Count")]
        public int RetryCount { get; set; } = 0;

        [Display(Name = "Max Retries")]
        public int MaxRetries { get; set; } = 3;

        [Display(Name = "Priority")]
        public int Priority { get; set; } = 5; // 1 = Highest, 10 = Lowest

        [Display(Name = "Scheduled Send Date")]
        public DateTime? ScheduledSendDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [StringLength(100)]
        public string? CreatedBy { get; set; }

        [Display(Name = "Opened Date")]
        public DateTime? OpenedDate { get; set; }

        [Display(Name = "Open Count")]
        public int OpenCount { get; set; } = 0;

        [Display(Name = "Tracking ID")]
        [StringLength(100)]
        public string? TrackingId { get; set; }

        [Display(Name = "Related Entity Type")]
        [StringLength(100)]
        public string? RelatedEntityType { get; set; }

        [Display(Name = "Related Entity ID")]
        [StringLength(100)]
        public string? RelatedEntityId { get; set; }
    }
}
