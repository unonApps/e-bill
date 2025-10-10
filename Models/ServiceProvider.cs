using System.ComponentModel.DataAnnotations;

namespace TAB.Web.Models
{
    public class ServiceProvider
    {
        public int Id { get; set; }

        [Required]
        [StringLength(10)]
        [Display(Name = "SP ID")]
        public string SPID { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Service Provider")]
        public string ServiceProviderName { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Main Contact Person")]
        public string SPMainCP { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(300)]
        [Display(Name = "Main Contact Email")]
        public string SPMainCPEmail { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Other Contact Emails")]
        public string? SPOtherCPsEmail { get; set; }

        [Required]
        [Display(Name = "Status")]
        public ServiceProviderStatus SPStatus { get; set; } = ServiceProviderStatus.Active;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public enum ServiceProviderStatus
    {
        Active = 1,
        Inactive = 0
    }
} 