using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace TAB.Web.Models
{
    public enum UserStatus
    {
        Active = 1,
        Inactive = 0
    }

    public class ApplicationUser : IdentityUser
    {
        // Add any additional user properties here
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool RequirePasswordChange { get; set; } = false;

        // User status (Active/Inactive)
        public UserStatus Status { get; set; } = UserStatus.Active;

        // Azure AD Authentication Properties
        [MaxLength(100)]
        [Display(Name = "Azure AD Object ID")]
        public string? AzureAdObjectId { get; set; }  // Unique identifier from Azure AD

        [MaxLength(100)]
        [Display(Name = "Azure AD Tenant ID")]
        public string? AzureAdTenantId { get; set; }

        [MaxLength(200)]
        [Display(Name = "Azure AD UPN")]
        public string? AzureAdUpn { get; set; }  // User Principal Name (username@domain.com)

        // Additional Azure AD Profile Information
        [MaxLength(200)]
        [Display(Name = "Job Title")]
        public string? JobTitle { get; set; }

        [MaxLength(200)]
        [Display(Name = "Department")]
        public string? Department { get; set; }

        [MaxLength(200)]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [MaxLength(50)]
        [Display(Name = "Mobile Phone")]
        public string? MobilePhone { get; set; }

        [MaxLength(200)]
        [Display(Name = "Office Location")]
        public string? OfficeLocation { get; set; }

        // Optional link to EbillUser (if this system user is also a staff member with billing records)
        [Display(Name = "E-Bill User")]
        public int? EbillUserId { get; set; }

        [Display(Name = "E-Bill User")]
        public virtual EbillUser? EbillUser { get; set; }

        // Organization and Office relationships
        public int? OrganizationId { get; set; }
        public int? OfficeId { get; set; }
        public int? SubOfficeId { get; set; }

        // Navigation properties
        public virtual Organization? Organization { get; set; }
        public virtual Office? Office { get; set; }
        public virtual SubOffice? SubOffice { get; set; }
    }
} 