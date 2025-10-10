using System.ComponentModel.DataAnnotations;

namespace TAB.Web.Models
{
    /// <summary>
    /// Unified model for displaying different types of requests in supervisor dashboard
    /// </summary>
    public class UnifiedRequest
    {
        public int Id { get; set; }
        public RequestType RequestType { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Organization { get; set; } = string.Empty;
        public string Office { get; set; } = string.Empty;
        public string RequestTitle { get; set; } = string.Empty;
        public string RequestDescription { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public int DaysOld => (DateTime.UtcNow - RequestDate).Days;
        public string Priority { get; set; } = "Normal";
        public string PriorityColor => DaysOld > 7 ? "danger" : DaysOld > 3 ? "warning" : "primary";
        public string PriorityIcon => DaysOld > 7 ? "exclamation-triangle" : DaysOld > 3 ? "clock" : "info-circle";
        public string Status { get; set; } = string.Empty;
        
        // Service Provider information for ICTS processing
        public ServiceProvider? ServiceProvider { get; set; }
        
        // Original object reference for accessing specific fields
        public object? OriginalRequest { get; set; }
    }

    public enum RequestType
    {
        [Display(Name = "SIM Card Request")]
        SimCard = 1,
        [Display(Name = "Device Refund Request")]
        DeviceRefund = 2,
        [Display(Name = "E-Bill Request")]
        EBill = 3
    }
} 