using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TAB.Web.Models
{
    /// <summary>
    /// Stores monthly KES to USD exchange rates for telecom billing conversion
    /// </summary>
    public class ExchangeRate
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Range(1, 12)]
        public int Month { get; set; }

        [Required]
        [Range(2000, 2100)]
        public int Year { get; set; }

        /// <summary>
        /// Exchange rate: 1 USD = X KES
        /// Example: If rate is 150, then 1 USD = 150 KES
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,4)")]
        [Range(0.01, 999999.9999)]
        public decimal Rate { get; set; }

        // Audit fields
        [Required]
        [StringLength(256)]
        public string CreatedBy { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [StringLength(256)]
        public string? UpdatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        // Helper properties for display
        [NotMapped]
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM");

        [NotMapped]
        public string PeriodDisplay => $"{MonthName} {Year}";
    }
}
