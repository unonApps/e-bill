using System.ComponentModel.DataAnnotations;

namespace TAB.Web.Models
{
    public class SubOffice
    {
        public int Id { get; set; }

        // Public-facing GUID for URLs and APIs
        public Guid PublicId { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(10)]
        public string? Code { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? ContactPerson { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        [Required]
        public int OfficeId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ModifiedDate { get; set; }

        // Navigation properties
        public virtual Office Office { get; set; } = null!;
        public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }
}