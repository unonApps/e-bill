using System.ComponentModel.DataAnnotations;

namespace TAB.Web.Models
{
    public class Organization
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
        
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation property for related offices
        public virtual ICollection<Office> Offices { get; set; } = new List<Office>();

        // Navigation property for related users
        public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }
} 