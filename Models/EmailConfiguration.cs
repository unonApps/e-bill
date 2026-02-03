using System.ComponentModel.DataAnnotations;

namespace TAB.Web.Models
{
    /// <summary>
    /// Database-stored email configuration settings
    /// </summary>
    public class EmailConfiguration
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "SMTP Server is required")]
        [Display(Name = "SMTP Server")]
        [StringLength(255)]
        public string SmtpServer { get; set; } = string.Empty;

        [Required(ErrorMessage = "SMTP Port is required")]
        [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
        [Display(Name = "SMTP Port")]
        public int SmtpPort { get; set; } = 587;

        [Required(ErrorMessage = "From Email Address is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "From Email Address")]
        [StringLength(255)]
        public string FromEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "From Display Name is required")]
        [Display(Name = "From Display Name")]
        [StringLength(255)]
        public string FromName { get; set; } = string.Empty;

        [Display(Name = "Username")]
        [StringLength(255)]
        public string? Username { get; set; }

        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        [StringLength(500)]
        public string? Password { get; set; }

        [Display(Name = "Enable SSL")]
        public bool EnableSsl { get; set; } = true;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Use Default Credentials")]
        public bool UseDefaultCredentials { get; set; } = false;

        [Display(Name = "Timeout (seconds)")]
        [Range(1, 300, ErrorMessage = "Timeout must be between 1 and 300 seconds")]
        public int Timeout { get; set; } = 30;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        [StringLength(100)]
        public string? ModifiedBy { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
