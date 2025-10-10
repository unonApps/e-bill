using System.ComponentModel.DataAnnotations;

namespace TAB.Web.Models
{
    public class AnomalyType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public SeverityLevel Severity { get; set; } = SeverityLevel.Low;

        public bool AutoReject { get; set; } = false;

        public bool IsActive { get; set; } = true;

        // Helper Methods
        public string GetSeverityBadgeClass()
        {
            return Severity switch
            {
                SeverityLevel.Low => "badge-info",
                SeverityLevel.Medium => "badge-warning",
                SeverityLevel.High => "badge-danger",
                SeverityLevel.Critical => "badge-dark",
                _ => "badge-secondary"
            };
        }

        public string GetSeverityIcon()
        {
            return Severity switch
            {
                SeverityLevel.Low => "bi-info-circle",
                SeverityLevel.Medium => "bi-exclamation-triangle",
                SeverityLevel.High => "bi-exclamation-octagon",
                SeverityLevel.Critical => "bi-x-octagon-fill",
                _ => "bi-question-circle"
            };
        }
    }

    public enum SeverityLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    // Static class for common anomaly codes
    public static class AnomalyCodes
    {
        public const string NoUser = "NO_USER";
        public const string InactiveUser = "INACTIVE_USER";
        public const string HighCost = "HIGH_COST";
        public const string Duplicate = "DUPLICATE";
        public const string InvalidNumber = "INVALID_NUMBER";
        public const string FutureDate = "FUTURE_DATE";
        public const string ExcessiveDuration = "EXCESSIVE_DURATION";
        public const string NoSupervisor = "NO_SUPERVISOR";
        public const string UnauthorizedIntl = "UNAUTHORIZED_INTL";
        public const string AfterHours = "AFTER_HOURS";
        public const string NoPhone = "NO_PHONE"; // Phone not registered in UserPhones
    }
}