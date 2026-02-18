using System.ComponentModel.DataAnnotations;

namespace TAB.Web.Models
{
    public class SimRequest
    {
        public int Id { get; set; }

        public Guid PublicId { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(20)]
        [Display(Name = "Index No")]
        public string IndexNo { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Organization")]
        public string Organization { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        [Display(Name = "Office")]
        public string Office { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Grade")]
        public string Grade { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        [Display(Name = "Functional Title")]
        public string FunctionalTitle { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Office Extension")]
        public string? OfficeExtension { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(300)]
        [Display(Name = "Official Email")]
        public string OfficialEmail { get; set; } = string.Empty;

        [Required]
        [Display(Name = "SIM Type")]
        public SimType SimType { get; set; }

        [Required]
        [Display(Name = "Line Request Type")]
        public LineRequestType LineRequestType { get; set; } = LineRequestType.NewLine;

        [StringLength(20)]
        [Display(Name = "Existing Phone Number")]
        public string? ExistingPhoneNumber { get; set; }

        [Display(Name = "Service Provider")]
        public int? ServiceProviderId { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Supervisor")]
        public string Supervisor { get; set; } = string.Empty;

        [StringLength(1000)]
        [Display(Name = "Previously Assigned Lines")]
        public string? PreviouslyAssignedLines { get; set; }

        [StringLength(500)]
        [Display(Name = "Remarks")]
        public string? Remarks { get; set; }

        [Required]
        [Display(Name = "Status")]
        public RequestStatus Status { get; set; } = RequestStatus.Draft;

        [Display(Name = "Request Date")]
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Processed Date")]
        public DateTime? ProcessedDate { get; set; }

        [StringLength(450)]
        [Display(Name = "Requested By")]
        public string RequestedBy { get; set; } = string.Empty;

        [StringLength(450)]
        [Display(Name = "Processed By")]
        public string? ProcessedBy { get; set; }

        [StringLength(500)]
        [Display(Name = "Processing Notes")]
        public string? ProcessingNotes { get; set; }

        [Display(Name = "Submitted to Supervisor")]
        public bool SubmittedToSupervisor { get; set; } = false;

        [Display(Name = "Supervisor Approval Date")]
        public DateTime? SupervisorApprovalDate { get; set; }

        [StringLength(500)]
        [Display(Name = "Supervisor Notes")]
        public string? SupervisorNotes { get; set; }

        // Class of Service fields (filled during supervisor approval)
        [StringLength(100)]
        [Display(Name = "Mobile Service")]
        public string? MobileService { get; set; }

        [StringLength(100)]
        [Display(Name = "Mobile Service Allowance")]
        public string? MobileServiceAllowance { get; set; }

        [StringLength(100)]
        [Display(Name = "Handset Allowance")]
        public string? HandsetAllowance { get; set; }

        [StringLength(200)]
        [Display(Name = "Supervisor Remarks")]
        public string? SupervisorRemarks { get; set; }

        [StringLength(300)]
        [Display(Name = "Supervisor Name")]
        public string? SupervisorName { get; set; }

        [StringLength(300)]
        [Display(Name = "Supervisor Email")]
        public string? SupervisorEmail { get; set; }

        // ICTS processing fields
        [StringLength(50)]
        [Display(Name = "SIM Serial No")]
        public string? SimSerialNo { get; set; }

        [StringLength(50)]
        [Display(Name = "Service Request No")]
        public string? ServiceRequestNo { get; set; }

        [StringLength(20)]
        [Display(Name = "Line Type")]
        public string? LineType { get; set; }

        [StringLength(20)]
        [Display(Name = "SIM PUK")]
        public string? SimPuk { get; set; }

        [StringLength(20)]
        [Display(Name = "Line Usage")]
        public string? LineUsage { get; set; }

        [StringLength(500)]
        [Display(Name = "Previous Lines")]
        public string? PreviousLines { get; set; }

        [Display(Name = "SP Notified Date")]
        public DateTime? SpNotifiedDate { get; set; }

        [StringLength(50)]
        [Display(Name = "Assigned No")]
        public string? AssignedNo { get; set; }

        [Display(Name = "Collection Notified Date")]
        public DateTime? CollectionNotifiedDate { get; set; }

        [StringLength(100)]
        [Display(Name = "SIM Issued By")]
        public string? SimIssuedBy { get; set; }

        [StringLength(100)]
        [Display(Name = "SIM Collected By")]
        public string? SimCollectedBy { get; set; }

        [Display(Name = "SIM Collected Date")]
        public DateTime? SimCollectedDate { get; set; }

        [StringLength(200)]
        [Display(Name = "ICTS Remark")]
        public string? IctsRemark { get; set; }

        // Navigation properties
        public virtual Models.ServiceProvider? ServiceProvider { get; set; }
        public virtual ICollection<SimRequestHistory> History { get; set; } = new List<SimRequestHistory>();
    }

    public enum SimType
    {
        [Display(Name = "Physical SIM")]
        Physical = 1,
        [Display(Name = "eSIM")]
        ESim = 2
    }

    public enum LineRequestType
    {
        [Display(Name = "New Line")]
        NewLine = 1,
        [Display(Name = "Existing Line")]
        ExistingLine = 2
    }

    public enum MobileServiceType
    {
        [Display(Name = "LOCAL (VOICE)")]
        LocalVoice = 1,
        [Display(Name = "LOCAL (VOICE + DATA)")]
        LocalVoiceData = 2,
        [Display(Name = "VOICE (LOCAL + INTERNATIONAL)/DATA (LOCAL + INTERNATIONAL)")]
        VoiceLocalInternationalDataLocalInternational = 3,
        [Display(Name = "Voice (local + international)/Data (local + international)")]
        VoiceLocalInternationalDataLocalInternationalLower = 4
    }

    public enum RequestStatus
    {
        [Display(Name = "Draft")]
        Draft = 0,
        [Display(Name = "Pending Supervisor Approval")]
        PendingSupervisor = 1,
        [Display(Name = "Pending UNON/ICTS")]
        PendingIcts = 2,
        [Display(Name = "Pending Admin Approval")]
        PendingAdmin = 3,
        [Display(Name = "Pending Service Provider SIM Issuance")]
        PendingServiceProvider = 7,
        [Display(Name = "Pending SIM Collection")]
        PendingSIMCollection = 8,
        [Display(Name = "Approved")]
        Approved = 4,
        [Display(Name = "Rejected")]
        Rejected = 5,
        [Display(Name = "Completed")]
        Completed = 6,
        [Display(Name = "Cancelled")]
        Cancelled = 9
    }
} 