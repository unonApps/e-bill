using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAB.Web.Models
{
    public class ClassOfService
    {
        public int Id { get; set; }

        // Public-facing GUID for URLs and APIs
        public Guid PublicId { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string Class { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string Service { get; set; } = string.Empty;
        
        [Required]
        [StringLength(200)]
        public string EligibleStaff { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string? AirtimeAllowance { get; set; }
        
        [StringLength(50)]
        public string? DataAllowance { get; set; }
        
        [StringLength(50)]
        public string? HandsetAllowance { get; set; }
        
        [StringLength(500)]
        public string? HandsetAIRemarks { get; set; }

        // Numeric Allowance Fields for Calculations
        [Column(TypeName = "decimal(18,4)")]
        public decimal? AirtimeAllowanceAmount { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? DataAllowanceAmount { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal? HandsetAllowanceAmount { get; set; }

        [StringLength(20)]
        public string BillingPeriod { get; set; } = "Monthly";

        public ServiceStatus ServiceStatus { get; set; } = ServiceStatus.Active;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        // Display property for status
        public string ServiceStatusDisplay => ServiceStatus == ServiceStatus.Active ? "Active" : "Inactive";

        // Navigation property - One ClassOfService can be assigned to many UserPhones
        public virtual ICollection<UserPhone> UserPhones { get; set; } = new List<UserPhone>();
    }
    
    public enum ServiceStatus
    {
        Inactive = 0,
        Active = 1
    }
} 