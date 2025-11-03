using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAB.Web.Models
{
    /// <summary>
    /// System-wide configuration for recovery rules and automation
    /// </summary>
    [Table("RecoveryConfiguration")]
    public class RecoveryConfiguration
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Name of the configuration rule
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string RuleName { get; set; } = string.Empty;

        /// <summary>
        /// Type of rule (Deadline, Automation, Notification, etc.)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string RuleType { get; set; } = string.Empty;

        /// <summary>
        /// Whether this rule is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Default number of days for verification deadline
        /// </summary>
        public int? DefaultVerificationDays { get; set; }

        /// <summary>
        /// Default number of days for approval deadline
        /// </summary>
        public int? DefaultApprovalDays { get; set; }

        /// <summary>
        /// Default number of days for revert (re-verification) deadline
        /// </summary>
        public int? DefaultRevertDays { get; set; }

        /// <summary>
        /// Maximum number of times a verification can be reverted
        /// </summary>
        public int MaxRevertsAllowed { get; set; } = 2;

        /// <summary>
        /// Job execution interval in minutes
        /// </summary>
        public int? JobIntervalMinutes { get; set; }

        /// <summary>
        /// Whether automation is enabled for this rule
        /// </summary>
        public bool AutomationEnabled { get; set; } = true;

        /// <summary>
        /// Whether manual approval is required before automation executes
        /// </summary>
        public bool RequireApprovalForAutomation { get; set; } = false;

        /// <summary>
        /// Whether notifications are enabled
        /// </summary>
        public bool NotificationEnabled { get; set; } = true;

        /// <summary>
        /// How many days before deadline to send reminders
        /// </summary>
        public int? ReminderDaysBefore { get; set; }

        /// <summary>
        /// Whether to send email notifications in addition to in-app notifications
        /// </summary>
        public bool EnableEmailNotifications { get; set; } = false;

        /// <summary>
        /// Admin email address for job execution notifications
        /// </summary>
        [MaxLength(200)]
        public string? AdminNotificationEmail { get; set; }

        /// <summary>
        /// Additional configuration in JSON format
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? ConfigValue { get; set; }

        /// <summary>
        /// Description of what this configuration does
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// When this configuration was created
        /// </summary>
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Who created this configuration
        /// </summary>
        [MaxLength(100)]
        public string? CreatedBy { get; set; }

        /// <summary>
        /// When this configuration was last modified
        /// </summary>
        public DateTime? ModifiedDate { get; set; }

        /// <summary>
        /// When this configuration was last modified (alias for ModifiedDate)
        /// </summary>
        [NotMapped]
        public DateTime? LastModifiedDate
        {
            get => ModifiedDate;
            set => ModifiedDate = value;
        }

        /// <summary>
        /// Who last modified this configuration
        /// </summary>
        [MaxLength(100)]
        public string? ModifiedBy { get; set; }

        /// <summary>
        /// Who last modified this configuration (alias for ModifiedBy)
        /// </summary>
        [NotMapped]
        public string? LastModifiedBy
        {
            get => ModifiedBy;
            set => ModifiedBy = value;
        }
    }
}
