using System.ComponentModel.DataAnnotations;

namespace TAB.Web.Models
{
    public class EbillUser
    {
        public int Id { get; set; }

        // Public-facing GUID for URLs and APIs (non-sequential for security)
        public Guid PublicId { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Index Number")]
        public string IndexNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Official Mobile Number")]
        [Phone]
        public string OfficialMobileNumber { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Issued Device ID")]
        public string? IssuedDeviceID { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Location")]
        public string? Location { get; set; }

        // Foreign key relationships
        [Display(Name = "Organization")]
        public int? OrganizationId { get; set; }

        [Display(Name = "Office")]
        public int? OfficeId { get; set; }

        [Display(Name = "Sub Office")]
        public int? SubOfficeId { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [StringLength(50)]
        [Display(Name = "Supervisor Index Number")]
        public string? SupervisorIndexNumber { get; set; }

        [StringLength(200)]
        [Display(Name = "Supervisor Name")]
        public string? SupervisorName { get; set; }

        [EmailAddress]
        [StringLength(256)]
        [Display(Name = "Supervisor Email")]
        public string? SupervisorEmail { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastModifiedDate { get; set; }

        // Authentication - Link to ApplicationUser for login
        [StringLength(450)]
        [Display(Name = "Application User ID")]
        public string? ApplicationUserId { get; set; }

        [Display(Name = "Has Login Account")]
        public bool HasLoginAccount { get; set; } = false;

        [Display(Name = "Login Enabled")]
        public bool LoginEnabled { get; set; } = false;

        // Computed property for full name
        public string FullName => $"{FirstName} {LastName}";

        // Navigation properties
        public virtual Organization? OrganizationEntity { get; set; }
        public virtual Office? OfficeEntity { get; set; }
        public virtual SubOffice? SubOfficeEntity { get; set; }
        public virtual ApplicationUser? ApplicationUser { get; set; }
    }
} 