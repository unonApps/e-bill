using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAB.Web.Models
{
    public class UserPhone
    {
        public int Id { get; set; }

        // Public-facing GUID for URLs and APIs
        public Guid PublicId { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(50)]
        [Display(Name = "Index Number")]
        public string IndexNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Phone Type")]
        public string PhoneType { get; set; } = "Mobile"; // Mobile, Desk, Extension, Home, Temporary

        [Display(Name = "Primary Phone")]
        public bool IsPrimary { get; set; } = false;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Required]
        [Display(Name = "Status")]
        public PhoneStatus Status { get; set; } = PhoneStatus.Active;

        [Display(Name = "Assigned Date")]
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Unassigned Date")]
        public DateTime? UnassignedDate { get; set; }

        [StringLength(200)]
        [Display(Name = "Location")]
        public string? Location { get; set; }

        [StringLength(500)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [StringLength(100)]
        [Display(Name = "Created By")]
        public string? CreatedBy { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Foreign key for ClassOfService
        public int? ClassOfServiceId { get; set; }

        // Navigation properties
        [ForeignKey("IndexNumber")]
        public virtual EbillUser? EbillUser { get; set; }

        [ForeignKey("ClassOfServiceId")]
        public virtual ClassOfService? ClassOfService { get; set; }

        // Computed properties
        [NotMapped]
        public string PhoneTypeDisplay => PhoneType switch
        {
            "Mobile" => "📱 Mobile",
            "Desk" => "☎️ Desk",
            "Extension" => "📞 Extension",
            "Home" => "🏠 Home",
            "Temporary" => "⏰ Temporary",
            _ => PhoneType
        };

        [NotMapped]
        public string StatusBadge => IsActive ? "Active" : "Inactive";

        [NotMapped]
        public string StatusColor => IsActive ? "success" : "secondary";

        [NotMapped]
        public string StatusBadgeText => Status switch
        {
            PhoneStatus.Active => "Active",
            PhoneStatus.Suspended => "Suspended",
            PhoneStatus.Deactivated => "Deactivated",
            _ => "Unknown"
        };

        [NotMapped]
        public string StatusBadgeColor => Status switch
        {
            PhoneStatus.Active => "success",
            PhoneStatus.Suspended => "warning",
            PhoneStatus.Deactivated => "danger",
            _ => "secondary"
        };
    }

    public enum PhoneStatus
    {
        [Display(Name = "Active")]
        Active = 1,

        [Display(Name = "Suspended")]
        Suspended = 2,

        [Display(Name = "Deactivated")]
        Deactivated = 3
    }

    // Enum for phone types (optional, for stronger typing)
    public static class PhoneTypes
    {
        public const string Mobile = "Mobile";
        public const string Desk = "Desk";
        public const string Extension = "Extension";
        public const string Home = "Home";
        public const string Temporary = "Temporary";
        public const string Conference = "Conference";
        public const string Fax = "Fax";

        public static List<string> AllTypes => new()
        {
            Mobile, Desk, Extension, Home, Temporary, Conference, Fax
        };
    }
}