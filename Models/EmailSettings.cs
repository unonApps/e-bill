using System.ComponentModel.DataAnnotations;

namespace TAB.Web.Models
{
    public class EmailSettings
    {
        [Required(ErrorMessage = "SMTP Server is required")]
        [Display(Name = "SMTP Server")]
        public string SmtpServer { get; set; } = string.Empty;

        [Required(ErrorMessage = "SMTP Port is required")]
        [Range(1, 65535, ErrorMessage = "Port must be between 1 and 65535")]
        [Display(Name = "SMTP Port")]
        public int SmtpPort { get; set; } = 587;

        [Required(ErrorMessage = "From Email Address is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "From Email Address")]
        public string FromEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "From Display Name is required")]
        [Display(Name = "From Display Name")]
        public string FromName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Enable SSL")]
        public bool EnableSsl { get; set; } = true;
    }
} 