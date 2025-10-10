using System.ComponentModel.DataAnnotations;

namespace TAB.Web.Models
{
    public class Office
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

        [Required]
        public int OrganizationId { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Organization Organization { get; set; } = null!;
        public virtual ICollection<SubOffice> SubOffices { get; set; } = new List<SubOffice>();
        public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    }
}