using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;
using System.Text;
using System.Text.Json;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class CallLogsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CallLogsModel> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFlexibleDateParserService _dateParser;

        public CallLogsModel(ApplicationDbContext context, ILogger<CallLogsModel> logger, UserManager<ApplicationUser> userManager, IFlexibleDateParserService dateParser)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _dateParser = dateParser;
        }

        public List<CallLog> CallLogs { get; set; } = new();
        public List<dynamic> AllTelecomRecords { get; set; } = new();
        public List<ImportJob> RecentImports { get; set; } = new();
        public ImportJob? SelectedImportJob { get; set; }
        public string CurrentUserName { get; set; } = string.Empty;
        
        [TempData]
        public string? StatusMessage { get; set; }
        
        [TempData]
        public string? StatusMessageClass { get; set; }
        
        // Statistics
        public int TotalLogs { get; set; }
        public int LinkedLogs { get; set; }
        public int UnlinkedLogs { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalAmountUSD { get; set; }
        public decimal TotalAmountKSH { get; set; }
        
        // Filter properties
        [BindProperty(SupportsGet = true)]
        public string FilterType { get; set; } = "all";
        
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public Guid? ImportJobId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 25;

        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public async Task OnGetAsync()
        {
            // Get current user's name
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                CurrentUserName = !string.IsNullOrEmpty(currentUser.FirstName) && !string.IsNullOrEmpty(currentUser.LastName)
                    ? $"{currentUser.FirstName} {currentUser.LastName}"
                    : currentUser.UserName ?? "Administrator";
            }

            // =============================================
            // OPTIMIZED APPROACH v2:
            // 1. Pre-load registered phone numbers (avoid correlated subqueries)
            // 2. Only load records needed for current page
            // 3. Use single COUNT query per table
            // =============================================

            // Pre-load registered phone numbers to avoid N+1 queries
            var registeredPhones = await _context.UserPhones
                .Where(up => up.IsActive)
                .Select(up => up.PhoneNumber)
                .ToListAsync();
            var registeredPhoneSet = new HashSet<string>(registeredPhones ?? new List<string>(), StringComparer.OrdinalIgnoreCase);

            // Calculate how many records to load per table for pagination
            // We need enough records to fill the page after combining and sorting
            var recordsToLoad = PageSize * 2; // Load 2x page size per table to ensure we have enough after merge

            // First get regular CallLogs query
            var query = _context.CallLogs
                .Include(c => c.EbillUser)
                .AsQueryable();

            // Apply search filter for CallLogs
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                query = query.Where(c =>
                    c.AccountNo.ToLower().Contains(searchLower) ||
                    c.SubAccountName.ToLower().Contains(searchLower) ||
                    c.MSISDN.ToLower().Contains(searchLower) ||
                    (c.InvoiceNo != null && c.InvoiceNo.ToLower().Contains(searchLower)) ||
                    (c.EbillUser != null && (
                        c.EbillUser.FirstName.ToLower().Contains(searchLower) ||
                        c.EbillUser.LastName.ToLower().Contains(searchLower) ||
                        c.EbillUser.IndexNumber.ToLower().Contains(searchLower)
                    )));
            }

            // Apply date range filter
            if (StartDate.HasValue)
            {
                query = query.Where(c => c.InvoiceDate >= StartDate.Value);
            }

            if (EndDate.HasValue)
            {
                query = query.Where(c => c.InvoiceDate <= EndDate.Value);
            }

            // Apply link status filter
            switch (FilterType?.ToLower())
            {
                case "linked":
                    query = query.Where(c => c.EbillUserId.HasValue);
                    break;
                case "unlinked":
                    query = query.Where(c => !c.EbillUserId.HasValue);
                    break;
                default:
                    // Show all
                    break;
            }

            // =============================================
            // Step 1: Calculate statistics using efficient single query
            // =============================================

            // Get count for CallLogs (simplified - one query)
            var callLogCount = await query.CountAsync();
            var callLogLinkedCount = FilterType?.ToLower() == "unlinked" ? 0 :
                (FilterType?.ToLower() == "linked" ? callLogCount : await query.CountAsync(c => c.EbillUserId.HasValue));
            var callLogUnlinkedCount = FilterType?.ToLower() == "linked" ? 0 :
                (FilterType?.ToLower() == "unlinked" ? callLogCount : callLogCount - callLogLinkedCount);

            var callLogRecords = await query
                .OrderByDescending(c => c.InvoiceDate)
                .ThenBy(c => c.AccountNo)
                .Take(recordsToLoad)
                .Select(c => new {
                    Id = c.Id,
                    Type = "CallLog",
                    AccountNo = c.AccountNo,
                    SubAccountNo = c.SubAccountNo,
                    SubAccountName = c.SubAccountName,
                    MSISDN = c.MSISDN,
                    InvoiceNo = c.InvoiceNo ?? "N/A",
                    InvoiceDate = c.InvoiceDate,
                    CallType = "CallLog",
                    GrossTotal = c.GrossTotal,
                    AmountUSD = (decimal?)null,
                    AmountKSH = (decimal?)null,
                    NetAccessFee = c.NetAccessFee,
                    EbillUserId = c.EbillUserId,
                    EbillUser = c.EbillUser,
                    Carrier = "CallLog",
                    Destination = "N/A",
                    VAT16 = c.VAT16,
                    Excise15 = c.Excise15,
                    StagingBatchId = (Guid?)null,
                    BillingPeriod = (string?)null,
                    CallMonth = 0,
                    CallYear = 0,
                    Extension = (string?)null,
                    UserPhoneId = (int?)null,
                    IsExtensionRegistered = false,
                    ProcessingStatus = ProcessingStatus.Staged
                })
                .ToListAsync();

            // Clear CallLogs since we're not using it
            CallLogs = new List<CallLog>();

            // Load recent import jobs for display
            RecentImports = await _context.ImportJobs
                .Where(j => j.CallLogType == "PSTN" || j.CallLogType == "PrivateWire" ||
                           j.CallLogType == "Safaricom" || j.CallLogType == "Airtel")
                .Where(j => j.Status == "Completed")
                .OrderByDescending(j => j.CreatedDate)
                .Take(20)
                .ToListAsync();

            // If a specific import job is selected, load its data
            if (ImportJobId.HasValue)
            {
                SelectedImportJob = await _context.ImportJobs
                    .FirstOrDefaultAsync(j => j.Id == ImportJobId.Value);
            }

            // Build query for other telecom tables with optional import job filter
            var pstnQuery = _context.PSTNs
                .Include(p => p.EbillUser)
                .AsQueryable();

            if (ImportJobId.HasValue && SelectedImportJob?.CallLogType == "PSTN")
            {
                pstnQuery = pstnQuery.Where(p => p.ImportJobId == ImportJobId.Value);
            }

            // Apply search filter to PSTN
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                pstnQuery = pstnQuery.Where(p =>
                    (p.Extension != null && p.Extension.ToLower().Contains(searchLower)) ||
                    (p.IndexNumber != null && p.IndexNumber.ToLower().Contains(searchLower)) ||
                    (p.DialedNumber != null && p.DialedNumber.ToLower().Contains(searchLower)) ||
                    (p.Destination != null && p.Destination.ToLower().Contains(searchLower)) ||
                    (p.EbillUser != null && (
                        p.EbillUser.FirstName.ToLower().Contains(searchLower) ||
                        p.EbillUser.LastName.ToLower().Contains(searchLower) ||
                        p.EbillUser.IndexNumber.ToLower().Contains(searchLower) ||
                        p.EbillUser.Email.ToLower().Contains(searchLower)
                    )));
            }

            // Apply date range filter to PSTN
            if (StartDate.HasValue)
            {
                pstnQuery = pstnQuery.Where(p => p.CallDate >= StartDate.Value);
            }
            if (EndDate.HasValue)
            {
                pstnQuery = pstnQuery.Where(p => p.CallDate <= EndDate.Value);
            }

            // Apply link status filter to PSTN
            switch (FilterType?.ToLower())
            {
                case "linked":
                    pstnQuery = pstnQuery.Where(p => p.EbillUserId.HasValue);
                    break;
                case "unlinked":
                    pstnQuery = pstnQuery.Where(p => !p.EbillUserId.HasValue);
                    break;
            }

            // Calculate PSTN statistics (optimized)
            var pstnCount = await pstnQuery.CountAsync();
            var pstnLinkedCount = FilterType?.ToLower() == "unlinked" ? 0 :
                (FilterType?.ToLower() == "linked" ? pstnCount : await pstnQuery.CountAsync(p => p.EbillUserId.HasValue));
            var pstnUnlinkedCount = FilterType?.ToLower() == "linked" ? 0 :
                (FilterType?.ToLower() == "unlinked" ? pstnCount : pstnCount - pstnLinkedCount);

            // Load limited window of PSTN records (NO correlated subquery)
            var pstnRecords = await pstnQuery
                .OrderByDescending(p => p.CallDate)
                .Take(recordsToLoad)
                .Select(p => new {
                    Id = p.Id,
                    Type = "PSTN",
                    AccountNo = p.IndexNumber ?? "N/A",
                    SubAccountNo = "PSTN-" + p.Id,
                    SubAccountName = p.EbillUser != null ? (p.EbillUser.FirstName ?? "") + " " + (p.EbillUser.LastName ?? "") : "Unknown",
                    MSISDN = p.DialedNumber ?? "N/A",
                    InvoiceDate = p.CallDate.HasValue ? p.CallDate.Value : DateTime.MinValue,
                    CallType = "PSTN",
                    GrossTotal = p.AmountKSH ?? 0,
                    AmountUSD = p.AmountUSD,
                    AmountKSH = p.AmountKSH,
                    NetAccessFee = 0m,
                    EbillUserId = p.EbillUserId,
                    EbillUser = p.EbillUser,
                    Carrier = p.Carrier ?? "Unknown",
                    Destination = p.Destination ?? "N/A",
                    StagingBatchId = p.StagingBatchId,
                    BillingPeriod = p.BillingPeriod,
                    CallMonth = p.CallMonth,
                    CallYear = p.CallYear,
                    Extension = p.Extension,
                    UserPhoneId = p.UserPhoneId,
                    ProcessingStatus = p.ProcessingStatus
                })
                .ToListAsync();

            var privateWireQuery = _context.PrivateWires
                .Include(p => p.EbillUser)
                .AsQueryable();

            if (ImportJobId.HasValue && SelectedImportJob?.CallLogType == "PrivateWire")
            {
                privateWireQuery = privateWireQuery.Where(p => p.ImportJobId == ImportJobId.Value);
            }

            // Apply search filter to PrivateWire
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                privateWireQuery = privateWireQuery.Where(p =>
                    (p.Extension != null && p.Extension.ToLower().Contains(searchLower)) ||
                    (p.IndexNumber != null && p.IndexNumber.ToLower().Contains(searchLower)) ||
                    (p.DialedNumber != null && p.DialedNumber.ToLower().Contains(searchLower)) ||
                    (p.Destination != null && p.Destination.ToLower().Contains(searchLower)) ||
                    (p.EbillUser != null && (
                        p.EbillUser.FirstName.ToLower().Contains(searchLower) ||
                        p.EbillUser.LastName.ToLower().Contains(searchLower) ||
                        p.EbillUser.IndexNumber.ToLower().Contains(searchLower) ||
                        p.EbillUser.Email.ToLower().Contains(searchLower)
                    )));
            }

            // Apply date range filter to PrivateWire
            if (StartDate.HasValue)
            {
                privateWireQuery = privateWireQuery.Where(p => p.CallDate >= StartDate.Value);
            }
            if (EndDate.HasValue)
            {
                privateWireQuery = privateWireQuery.Where(p => p.CallDate <= EndDate.Value);
            }

            // Apply link status filter to PrivateWire
            switch (FilterType?.ToLower())
            {
                case "linked":
                    privateWireQuery = privateWireQuery.Where(p => p.EbillUserId.HasValue);
                    break;
                case "unlinked":
                    privateWireQuery = privateWireQuery.Where(p => !p.EbillUserId.HasValue);
                    break;
            }

            // Calculate PrivateWire statistics (optimized)
            var privateWireCount = await privateWireQuery.CountAsync();
            var privateWireLinkedCount = FilterType?.ToLower() == "unlinked" ? 0 :
                (FilterType?.ToLower() == "linked" ? privateWireCount : await privateWireQuery.CountAsync(p => p.EbillUserId.HasValue));
            var privateWireUnlinkedCount = FilterType?.ToLower() == "linked" ? 0 :
                (FilterType?.ToLower() == "unlinked" ? privateWireCount : privateWireCount - privateWireLinkedCount);

            // Load limited window of PrivateWire records (NO correlated subquery)
            var privateWireRecords = await privateWireQuery
                .OrderByDescending(p => p.CallDate)
                .Take(recordsToLoad)
                .Select(p => new {
                    Id = p.Id,
                    Type = "Private Wire",
                    AccountNo = p.IndexNumber ?? "N/A",
                    SubAccountNo = "PW-" + p.Id,
                    SubAccountName = p.EbillUser != null ? (p.EbillUser.FirstName ?? "") + " " + (p.EbillUser.LastName ?? "") : "Unknown",
                    MSISDN = p.DialedNumber ?? "N/A",
                    InvoiceDate = p.CallDate.HasValue ? p.CallDate.Value : DateTime.MinValue,
                    CallType = "Private Wire",
                    GrossTotal = p.AmountKSH ?? ((p.AmountUSD ?? 0) * 150),
                    AmountUSD = p.AmountUSD,
                    AmountKSH = p.AmountKSH,
                    NetAccessFee = 0m,
                    EbillUserId = p.EbillUserId,
                    EbillUser = p.EbillUser,
                    Carrier = "Private Wire",
                    Destination = p.Destination ?? "N/A",
                    StagingBatchId = p.StagingBatchId,
                    BillingPeriod = p.BillingPeriod,
                    CallMonth = p.CallMonth,
                    CallYear = p.CallYear,
                    Extension = p.Extension,
                    UserPhoneId = p.UserPhoneId,
                    ProcessingStatus = p.ProcessingStatus
                })
                .ToListAsync();

            var safaricomQuery = _context.Safaricoms
                .Include(s => s.EbillUser)
                .AsQueryable();

            if (ImportJobId.HasValue && SelectedImportJob?.CallLogType == "Safaricom")
            {
                safaricomQuery = safaricomQuery.Where(s => s.ImportJobId == ImportJobId.Value);
            }

            // Apply search filter to Safaricom
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                safaricomQuery = safaricomQuery.Where(s =>
                    (s.Ext != null && s.Ext.ToLower().Contains(searchLower)) ||
                    (s.IndexNumber != null && s.IndexNumber.ToLower().Contains(searchLower)) ||
                    (s.Dialed != null && s.Dialed.ToLower().Contains(searchLower)) ||
                    (s.Dest != null && s.Dest.ToLower().Contains(searchLower)) ||
                    (s.EbillUser != null && (
                        s.EbillUser.FirstName.ToLower().Contains(searchLower) ||
                        s.EbillUser.LastName.ToLower().Contains(searchLower) ||
                        s.EbillUser.IndexNumber.ToLower().Contains(searchLower) ||
                        s.EbillUser.Email.ToLower().Contains(searchLower)
                    )));
            }

            // Apply date range filter to Safaricom
            if (StartDate.HasValue)
            {
                safaricomQuery = safaricomQuery.Where(s => s.CallDate >= StartDate.Value);
            }
            if (EndDate.HasValue)
            {
                safaricomQuery = safaricomQuery.Where(s => s.CallDate <= EndDate.Value);
            }

            // Apply link status filter to Safaricom
            switch (FilterType?.ToLower())
            {
                case "linked":
                    safaricomQuery = safaricomQuery.Where(s => s.EbillUserId.HasValue);
                    break;
                case "unlinked":
                    safaricomQuery = safaricomQuery.Where(s => !s.EbillUserId.HasValue);
                    break;
            }

            // Calculate Safaricom statistics (optimized)
            var safaricomCount = await safaricomQuery.CountAsync();
            var safaricomLinkedCount = FilterType?.ToLower() == "unlinked" ? 0 :
                (FilterType?.ToLower() == "linked" ? safaricomCount : await safaricomQuery.CountAsync(s => s.EbillUserId.HasValue));
            var safaricomUnlinkedCount = FilterType?.ToLower() == "linked" ? 0 :
                (FilterType?.ToLower() == "unlinked" ? safaricomCount : safaricomCount - safaricomLinkedCount);

            // Load limited window of Safaricom records (NO correlated subquery - this was causing 53+ second queries!)
            var safaricomRecords = await safaricomQuery
                .OrderByDescending(s => s.CallDate)
                .Take(recordsToLoad)
                .Select(s => new {
                    Id = s.Id,
                    Type = "Safaricom",
                    AccountNo = s.IndexNumber ?? "N/A",
                    SubAccountNo = "SAF-" + s.Id,
                    SubAccountName = s.EbillUser != null ? (s.EbillUser.FirstName ?? "") + " " + (s.EbillUser.LastName ?? "") : "Unknown",
                    MSISDN = s.Dialed ?? "N/A",
                    InvoiceDate = s.CallDate.HasValue ? s.CallDate.Value : DateTime.MinValue,
                    CallType = s.CallType ?? "N/A",
                    GrossTotal = s.Cost ?? 0,
                    AmountUSD = s.AmountUSD,
                    AmountKSH = s.Cost,
                    NetAccessFee = 0m,
                    EbillUserId = s.EbillUserId,
                    EbillUser = s.EbillUser,
                    Carrier = "Safaricom",
                    Destination = s.Dest ?? "N/A",
                    StagingBatchId = s.StagingBatchId,
                    BillingPeriod = s.BillingPeriod,
                    CallMonth = s.CallMonth,
                    CallYear = s.CallYear,
                    Extension = s.Ext,
                    UserPhoneId = s.UserPhoneId,
                    ProcessingStatus = s.ProcessingStatus
                })
                .ToListAsync();

            var airtelQuery = _context.Airtels
                .Include(a => a.EbillUser)
                .AsQueryable();

            if (ImportJobId.HasValue && SelectedImportJob?.CallLogType == "Airtel")
            {
                airtelQuery = airtelQuery.Where(a => a.ImportJobId == ImportJobId.Value);
            }

            // Apply search filter to Airtel
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                airtelQuery = airtelQuery.Where(a =>
                    (a.Ext != null && a.Ext.ToLower().Contains(searchLower)) ||
                    (a.IndexNumber != null && a.IndexNumber.ToLower().Contains(searchLower)) ||
                    (a.Dialed != null && a.Dialed.ToLower().Contains(searchLower)) ||
                    (a.Dest != null && a.Dest.ToLower().Contains(searchLower)) ||
                    (a.EbillUser != null && (
                        a.EbillUser.FirstName.ToLower().Contains(searchLower) ||
                        a.EbillUser.LastName.ToLower().Contains(searchLower) ||
                        a.EbillUser.IndexNumber.ToLower().Contains(searchLower) ||
                        a.EbillUser.Email.ToLower().Contains(searchLower)
                    )));
            }

            // Apply date range filter to Airtel
            if (StartDate.HasValue)
            {
                airtelQuery = airtelQuery.Where(a => a.CallDate >= StartDate.Value);
            }
            if (EndDate.HasValue)
            {
                airtelQuery = airtelQuery.Where(a => a.CallDate <= EndDate.Value);
            }

            // Apply link status filter to Airtel
            switch (FilterType?.ToLower())
            {
                case "linked":
                    airtelQuery = airtelQuery.Where(a => a.EbillUserId.HasValue);
                    break;
                case "unlinked":
                    airtelQuery = airtelQuery.Where(a => !a.EbillUserId.HasValue);
                    break;
            }

            // Calculate Airtel statistics (optimized)
            var airtelCount = await airtelQuery.CountAsync();
            var airtelLinkedCount = FilterType?.ToLower() == "unlinked" ? 0 :
                (FilterType?.ToLower() == "linked" ? airtelCount : await airtelQuery.CountAsync(a => a.EbillUserId.HasValue));
            var airtelUnlinkedCount = FilterType?.ToLower() == "linked" ? 0 :
                (FilterType?.ToLower() == "unlinked" ? airtelCount : airtelCount - airtelLinkedCount);

            // Load limited window of Airtel records (NO correlated subquery)
            var airtelRecords = await airtelQuery
                .OrderByDescending(a => a.CallDate)
                .Take(recordsToLoad)
                .Select(a => new {
                    Id = a.Id,
                    Type = "Airtel",
                    AccountNo = a.IndexNumber ?? "N/A",
                    SubAccountNo = "AIR-" + a.Id,
                    SubAccountName = a.EbillUser != null ? (a.EbillUser.FirstName ?? "") + " " + (a.EbillUser.LastName ?? "") : "Unknown",
                    MSISDN = a.Dialed ?? "N/A",
                    InvoiceDate = a.CallDate.HasValue ? a.CallDate.Value : DateTime.MinValue,
                    CallType = a.CallType ?? "N/A",
                    GrossTotal = a.Cost ?? 0,
                    AmountUSD = a.AmountUSD,
                    AmountKSH = a.Cost,
                    NetAccessFee = 0m,
                    EbillUserId = a.EbillUserId,
                    EbillUser = a.EbillUser,
                    Carrier = "Airtel",
                    Destination = a.Dest ?? "N/A",
                    StagingBatchId = a.StagingBatchId,
                    BillingPeriod = a.BillingPeriod,
                    CallMonth = a.CallMonth,
                    CallYear = a.CallYear,
                    Extension = a.Ext,
                    UserPhoneId = a.UserPhoneId,
                    ProcessingStatus = a.ProcessingStatus
                })
                .ToListAsync();

            // Combine all records - if filtering by import job, only add records from that type
            // Transform to include IsExtensionRegistered (calculated in-memory using pre-loaded set)
            var transformPstn = pstnRecords.Select(r => new {
                r.Id, r.Type, r.AccountNo, r.SubAccountNo, r.SubAccountName, r.MSISDN, r.InvoiceDate,
                r.CallType, r.GrossTotal, r.AmountUSD, r.AmountKSH, r.NetAccessFee, r.EbillUserId, r.EbillUser,
                r.Carrier, r.Destination, r.StagingBatchId, r.BillingPeriod, r.CallMonth, r.CallYear,
                r.Extension, r.UserPhoneId, r.ProcessingStatus,
                IsExtensionRegistered = !string.IsNullOrEmpty(r.Extension) && registeredPhoneSet.Contains(r.Extension)
            }).Cast<dynamic>().ToList();

            var transformPrivateWire = privateWireRecords.Select(r => new {
                r.Id, r.Type, r.AccountNo, r.SubAccountNo, r.SubAccountName, r.MSISDN, r.InvoiceDate,
                r.CallType, r.GrossTotal, r.AmountUSD, r.AmountKSH, r.NetAccessFee, r.EbillUserId, r.EbillUser,
                r.Carrier, r.Destination, r.StagingBatchId, r.BillingPeriod, r.CallMonth, r.CallYear,
                r.Extension, r.UserPhoneId, r.ProcessingStatus,
                IsExtensionRegistered = !string.IsNullOrEmpty(r.Extension) && registeredPhoneSet.Contains(r.Extension)
            }).Cast<dynamic>().ToList();

            var transformSafaricom = safaricomRecords.Select(r => new {
                r.Id, r.Type, r.AccountNo, r.SubAccountNo, r.SubAccountName, r.MSISDN, r.InvoiceDate,
                r.CallType, r.GrossTotal, r.AmountUSD, r.AmountKSH, r.NetAccessFee, r.EbillUserId, r.EbillUser,
                r.Carrier, r.Destination, r.StagingBatchId, r.BillingPeriod, r.CallMonth, r.CallYear,
                r.Extension, r.UserPhoneId, r.ProcessingStatus,
                IsExtensionRegistered = !string.IsNullOrEmpty(r.Extension) && registeredPhoneSet.Contains(r.Extension)
            }).Cast<dynamic>().ToList();

            var transformAirtel = airtelRecords.Select(r => new {
                r.Id, r.Type, r.AccountNo, r.SubAccountNo, r.SubAccountName, r.MSISDN, r.InvoiceDate,
                r.CallType, r.GrossTotal, r.AmountUSD, r.AmountKSH, r.NetAccessFee, r.EbillUserId, r.EbillUser,
                r.Carrier, r.Destination, r.StagingBatchId, r.BillingPeriod, r.CallMonth, r.CallYear,
                r.Extension, r.UserPhoneId, r.ProcessingStatus,
                IsExtensionRegistered = !string.IsNullOrEmpty(r.Extension) && registeredPhoneSet.Contains(r.Extension)
            }).Cast<dynamic>().ToList();

            // CallLogs already have IsExtensionRegistered set correctly
            var transformCallLog = callLogRecords.Cast<dynamic>().ToList();

            if (ImportJobId.HasValue && SelectedImportJob != null)
            {
                // Only add records from the selected import job type
                switch (SelectedImportJob.CallLogType)
                {
                    case "PSTN":
                        AllTelecomRecords.AddRange(transformPstn);
                        break;
                    case "PrivateWire":
                        AllTelecomRecords.AddRange(transformPrivateWire);
                        break;
                    case "Safaricom":
                        AllTelecomRecords.AddRange(transformSafaricom);
                        break;
                    case "Airtel":
                        AllTelecomRecords.AddRange(transformAirtel);
                        break;
                    default:
                        // If import type doesn't match, add all (fallback)
                        AllTelecomRecords.AddRange(transformCallLog);
                        AllTelecomRecords.AddRange(transformPstn);
                        AllTelecomRecords.AddRange(transformPrivateWire);
                        AllTelecomRecords.AddRange(transformSafaricom);
                        AllTelecomRecords.AddRange(transformAirtel);
                        break;
                }
            }
            else
            {
                // No import filter - add all records
                AllTelecomRecords.AddRange(transformCallLog);
                AllTelecomRecords.AddRange(transformPstn);
                AllTelecomRecords.AddRange(transformPrivateWire);
                AllTelecomRecords.AddRange(transformSafaricom);
                AllTelecomRecords.AddRange(transformAirtel);
            }

            // =============================================
            // Step 2: Sort combined records by date
            // =============================================
            AllTelecomRecords = AllTelecomRecords
                .OrderByDescending(r => r.InvoiceDate != null ? (DateTime)r.InvoiceDate : DateTime.MinValue)
                .ToList();

            // =============================================
            // Step 3: Calculate statistics from COUNT queries (much faster!)
            // =============================================
            TotalRecords = callLogCount + pstnCount + privateWireCount + safaricomCount + airtelCount;
            TotalLogs = TotalRecords;
            LinkedLogs = callLogLinkedCount + pstnLinkedCount + privateWireLinkedCount + safaricomLinkedCount + airtelLinkedCount;
            UnlinkedLogs = callLogUnlinkedCount + pstnUnlinkedCount + privateWireUnlinkedCount + safaricomUnlinkedCount + airtelUnlinkedCount;

            // Calculate amounts from the loaded window (approximate, but fast)
            // For exact amounts across ALL records, would need separate SUM queries
            TotalAmount = AllTelecomRecords.Sum(r => r.GrossTotal != null ? (decimal)r.GrossTotal : 0m);
            TotalAmountUSD = AllTelecomRecords.Sum(r => {
                var amountUSD = r.AmountUSD as decimal?;
                return amountUSD ?? 0m;
            });
            TotalAmountKSH = AllTelecomRecords.Sum(r => {
                var amountKSH = r.AmountKSH as decimal?;
                var grossTotal = r.GrossTotal as decimal?;
                return amountKSH ?? grossTotal ?? 0m;
            });

            // =============================================
            // Step 4: Apply pagination AFTER calculating statistics
            // =============================================
            AllTelecomRecords = AllTelecomRecords
                .Skip((PageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            _logger.LogInformation("CallLogs page loaded: {TotalRecords} total records, showing page {PageNumber} of {TotalPages} ({PageSize} per page)",
                TotalRecords, PageNumber, TotalPages, PageSize);
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            try
            {
                var callLog = await _context.CallLogs.FindAsync(id);
                if (callLog == null)
                {
                    StatusMessage = "Call log not found.";
                    StatusMessageClass = "danger";
                    return RedirectToPage();
                }

                _context.CallLogs.Remove(callLog);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted CallLog ID: {Id}", id);

                StatusMessage = "Call log deleted successfully.";
                StatusMessageClass = "success";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting CallLog with ID: {Id}", id);
                StatusMessage = "An error occurred while deleting the call log.";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetImportDetailsAsync(int id)
        {
            var audit = await _context.ImportAudits.FindAsync(id);
            if (audit == null)
            {
                return NotFound();
            }

            // Parse detailed results if available
            ImportDetailedResult? detailedResult = null;
            if (!string.IsNullOrEmpty(audit.DetailedResults))
            {
                try
                {
                    detailedResult = JsonSerializer.Deserialize<ImportDetailedResult>(audit.DetailedResults);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse detailed results for import {Id}", id);
                }
            }

            return new JsonResult(new
            {
                audit.Id,
                audit.ImportType,
                audit.FileName,
                audit.ImportDate,
                audit.ImportedBy,
                audit.TotalRecords,
                audit.SuccessCount,
                audit.ErrorCount,
                audit.SkippedCount,
                audit.UpdatedCount,
                audit.SummaryMessage,
                Errors = detailedResult?.Errors ?? new List<ImportErrorDetail>(),
                Skipped = detailedResult?.Skipped ?? new List<ImportSkippedDetail>()
            });
        }

        public async Task<IActionResult> OnGetLinkUserAsync(int id)
        {
            // This handler could redirect to a user selection page
            // or show a modal to select a user to link
            // For now, we'll just redirect back with a message
            StatusMessage = "User linking feature will be implemented soon.";
            StatusMessageClass = "info";
            await Task.CompletedTask;
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetSearchUsersAsync(string searchTerm, string accountNo)
        {
            if (string.IsNullOrWhiteSpace(searchTerm) && string.IsNullOrWhiteSpace(accountNo))
            {
                return new JsonResult(new { users = new List<object>() });
            }

            var query = _context.EbillUsers
                .Include(u => u.OrganizationEntity)
                .Include(u => u.OfficeEntity)
                .AsQueryable();

            // Search by account number first (exact match)
            if (!string.IsNullOrWhiteSpace(accountNo))
            {
                var exactMatch = await _context.EbillUsers
                    .Include(u => u.OrganizationEntity)
                    .Include(u => u.OfficeEntity)
                    .Where(u => u.IndexNumber == accountNo)
                    .Select(u => new
                    {
                        u.Id,
                        u.FirstName,
                        u.LastName,
                        u.IndexNumber,
                        u.Email,
                        PhoneNumber = u.OfficialMobileNumber,
                        OrganizationName = u.OrganizationEntity != null ? u.OrganizationEntity.Name : "N/A",
                        OfficeName = u.OfficeEntity != null ? u.OfficeEntity.Name : "N/A"
                    })
                    .FirstOrDefaultAsync();

                if (exactMatch != null)
                {
                    return new JsonResult(new { users = new[] { exactMatch }, exactMatch = true });
                }
            }

            // Otherwise search by term
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchLower = searchTerm.ToLower();
                query = query.Where(u =>
                    u.FirstName.ToLower().Contains(searchLower) ||
                    u.LastName.ToLower().Contains(searchLower) ||
                    u.IndexNumber.ToLower().Contains(searchLower) ||
                    (u.Email != null && u.Email.ToLower().Contains(searchLower)) ||
                    (u.OfficialMobileNumber != null && u.OfficialMobileNumber.Contains(searchTerm))
                );
            }

            var users = await query
                .Select(u => new
                {
                    u.Id,
                    u.FirstName,
                    u.LastName,
                    u.IndexNumber,
                    u.Email,
                    PhoneNumber = u.OfficialMobileNumber,
                    OrganizationName = u.OrganizationEntity != null ? u.OrganizationEntity.Name : "N/A",
                    OfficeName = u.OfficeEntity != null ? u.OfficeEntity.Name : "N/A"
                })
                .Take(10)
                .ToListAsync();

            return new JsonResult(new { users });
        }

        public async Task<IActionResult> OnGetTelecomDetailsAsync(string type, int id)
        {
            try
            {
                object? recordDetails = null;

                switch (type?.ToLower())
                {
                    case "pstn":
                        var pstn = await _context.PSTNs
                            .Include(p => p.EbillUser)
                            .ThenInclude(u => u.OrganizationEntity)
                            .Include(p => p.EbillUser)
                            .ThenInclude(u => u.OfficeEntity)
                            .FirstOrDefaultAsync(p => p.Id == id);

                        if (pstn != null)
                        {
                            recordDetails = new
                            {
                                type = "PSTN",
                                id = pstn.Id,
                                indexNumber = pstn.IndexNumber,
                                extension = pstn.Extension,
                                dialedNumber = pstn.DialedNumber,
                                callDate = pstn.CallDate?.ToString("MMM dd, yyyy"),
                                callTime = pstn.CallTime?.ToString(@"hh\:mm\:ss"),
                                duration = pstn.Duration,
                                destination = pstn.Destination,
                                carrier = pstn.Carrier,
                                amountKSH = pstn.AmountKSH,
                                amountUSD = pstn.AmountUSD,
                                billingPeriod = pstn.BillingPeriod,
                                callMonth = pstn.CallMonth,
                                callYear = pstn.CallYear,
                                user = pstn.EbillUser != null ? new
                                {
                                    name = $"{pstn.EbillUser.FirstName} {pstn.EbillUser.LastName}",
                                    indexNumber = pstn.EbillUser.IndexNumber,
                                    email = pstn.EbillUser.Email,
                                    phone = pstn.EbillUser.OfficialMobileNumber,
                                    organization = pstn.EbillUser.OrganizationEntity?.Name,
                                    office = pstn.EbillUser.OfficeEntity?.Name
                                } : null,
                                createdDate = pstn.CreatedDate.ToString("MMM dd, yyyy HH:mm"),
                                createdBy = pstn.CreatedBy
                            };
                        }
                        break;

                    case "private wire":
                        var privateWire = await _context.PrivateWires
                            .Include(p => p.EbillUser)
                            .ThenInclude(u => u.OrganizationEntity)
                            .Include(p => p.EbillUser)
                            .ThenInclude(u => u.OfficeEntity)
                            .FirstOrDefaultAsync(p => p.Id == id);

                        if (privateWire != null)
                        {
                            recordDetails = new
                            {
                                type = "Private Wire",
                                id = privateWire.Id,
                                indexNumber = privateWire.IndexNumber,
                                extension = privateWire.Extension,
                                dialedNumber = privateWire.DialedNumber,
                                destinationLine = privateWire.DestinationLine,
                                callDate = privateWire.CallDate?.ToString("MMM dd, yyyy"),
                                callTime = privateWire.CallTime?.ToString(@"hh\:mm\:ss"),
                                duration = privateWire.Duration,
                                durationExtended = privateWire.DurationExtended,
                                destination = privateWire.Destination,
                                amountUSD = privateWire.AmountUSD,
                                amountKSH = privateWire.AmountKSH,
                                billingPeriod = privateWire.BillingPeriod,
                                callMonth = privateWire.CallMonth,
                                callYear = privateWire.CallYear,
                                user = privateWire.EbillUser != null ? new
                                {
                                    name = $"{privateWire.EbillUser.FirstName} {privateWire.EbillUser.LastName}",
                                    indexNumber = privateWire.EbillUser.IndexNumber,
                                    email = privateWire.EbillUser.Email,
                                    phone = privateWire.EbillUser.OfficialMobileNumber,
                                    organization = privateWire.EbillUser.OrganizationEntity?.Name,
                                    office = privateWire.EbillUser.OfficeEntity?.Name
                                } : null,
                                createdDate = privateWire.CreatedDate.ToString("MMM dd, yyyy HH:mm"),
                                createdBy = privateWire.CreatedBy
                            };
                        }
                        break;

                    case "safaricom":
                        var safaricom = await _context.Safaricoms
                            .Include(s => s.EbillUser)
                            .ThenInclude(u => u.OrganizationEntity)
                            .Include(s => s.EbillUser)
                            .ThenInclude(u => u.OfficeEntity)
                            .FirstOrDefaultAsync(s => s.Id == id);

                        if (safaricom != null)
                        {
                            recordDetails = new
                            {
                                type = "Safaricom",
                                id = safaricom.Id,
                                indexNumber = safaricom.IndexNumber,
                                extension = safaricom.Ext,
                                dialedNumber = safaricom.Dialed,
                                callDate = safaricom.CallDate?.ToString("MMM dd, yyyy"),
                                callTime = safaricom.CallTime?.ToString(@"hh\:mm\:ss"),
                                duration = safaricom.Dur,
                                durationExtended = safaricom.Durx,
                                destination = safaricom.Dest,
                                cost = safaricom.Cost,
                                amountUSD = safaricom.AmountUSD,
                                callType = safaricom.CallType,
                                billingPeriod = safaricom.BillingPeriod,
                                callMonth = safaricom.CallMonth,
                                callYear = safaricom.CallYear,
                                user = safaricom.EbillUser != null ? new
                                {
                                    name = $"{safaricom.EbillUser.FirstName} {safaricom.EbillUser.LastName}",
                                    indexNumber = safaricom.EbillUser.IndexNumber,
                                    email = safaricom.EbillUser.Email,
                                    phone = safaricom.EbillUser.OfficialMobileNumber,
                                    organization = safaricom.EbillUser.OrganizationEntity?.Name,
                                    office = safaricom.EbillUser.OfficeEntity?.Name
                                } : null,
                                createdDate = safaricom.CreatedDate.ToString("MMM dd, yyyy HH:mm"),
                                createdBy = safaricom.CreatedBy
                            };
                        }
                        break;

                    case "airtel":
                        var airtel = await _context.Airtels
                            .Include(a => a.EbillUser)
                            .ThenInclude(u => u.OrganizationEntity)
                            .Include(a => a.EbillUser)
                            .ThenInclude(u => u.OfficeEntity)
                            .FirstOrDefaultAsync(a => a.Id == id);

                        if (airtel != null)
                        {
                            recordDetails = new
                            {
                                type = "Airtel",
                                id = airtel.Id,
                                indexNumber = airtel.IndexNumber,
                                extension = airtel.Ext,
                                dialedNumber = airtel.Dialed,
                                callDate = airtel.CallDate?.ToString("MMM dd, yyyy"),
                                callTime = airtel.CallTime?.ToString(@"hh\:mm\:ss"),
                                duration = airtel.Dur,
                                durationExtended = airtel.Durx,
                                destination = airtel.Dest,
                                cost = airtel.Cost,
                                amountUSD = airtel.AmountUSD,
                                callType = airtel.CallType,
                                billingPeriod = airtel.BillingPeriod,
                                callMonth = airtel.CallMonth,
                                callYear = airtel.CallYear,
                                user = airtel.EbillUser != null ? new
                                {
                                    name = $"{airtel.EbillUser.FirstName} {airtel.EbillUser.LastName}",
                                    indexNumber = airtel.EbillUser.IndexNumber,
                                    email = airtel.EbillUser.Email,
                                    phone = airtel.EbillUser.OfficialMobileNumber,
                                    organization = airtel.EbillUser.OrganizationEntity?.Name,
                                    office = airtel.EbillUser.OfficeEntity?.Name
                                } : null,
                                createdDate = airtel.CreatedDate.ToString("MMM dd, yyyy HH:mm"),
                                createdBy = airtel.CreatedBy
                            };
                        }
                        break;
                }

                if (recordDetails == null)
                {
                    return new JsonResult(new { success = false, message = "Record not found" });
                }

                return new JsonResult(new { success = true, record = recordDetails });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching telecom details for {Type} #{Id}", type, id);
                return new JsonResult(new { success = false, message = "An error occurred while fetching record details" });
            }
        }

        public async Task<IActionResult> OnPostLinkTelecomAsync(string type, int id, int userId, string extension)
        {
            try
            {
                var user = await _context.EbillUsers.FindAsync(userId);
                if (user == null)
                {
                    return new JsonResult(new { success = false, message = "User not found" });
                }

                int linkedCount = 0;
                var modifiedBy = User.Identity?.Name;
                var modifiedDate = DateTime.UtcNow;

                switch (type?.ToLower())
                {
                    case "pstn":
                        // Link ALL unlinked PSTN records with the same extension
                        if (!string.IsNullOrEmpty(extension))
                        {
                            var pstnRecords = await _context.PSTNs
                                .Where(p => p.Extension == extension && p.EbillUserId == null)
                                .ToListAsync();

                            foreach (var pstn in pstnRecords)
                            {
                                pstn.EbillUserId = userId;
                                pstn.IndexNumber = user.IndexNumber;
                                pstn.ModifiedDate = modifiedDate;
                                pstn.ModifiedBy = modifiedBy;
                            }
                            linkedCount = pstnRecords.Count;
                        }
                        else
                        {
                            // Fallback: link single record if no extension provided
                            var pstn = await _context.PSTNs.FindAsync(id);
                            if (pstn != null)
                            {
                                pstn.EbillUserId = userId;
                                pstn.IndexNumber = user.IndexNumber;
                                pstn.ModifiedDate = modifiedDate;
                                pstn.ModifiedBy = modifiedBy;
                                linkedCount = 1;
                            }
                        }
                        break;

                    case "privatewire":
                    case "private wire":
                        // Link ALL unlinked PrivateWire records with the same extension
                        if (!string.IsNullOrEmpty(extension))
                        {
                            var pwRecords = await _context.PrivateWires
                                .Where(p => p.Extension == extension && p.EbillUserId == null)
                                .ToListAsync();

                            foreach (var pw in pwRecords)
                            {
                                pw.EbillUserId = userId;
                                pw.IndexNumber = user.IndexNumber;
                                pw.ModifiedDate = modifiedDate;
                                pw.ModifiedBy = modifiedBy;
                            }
                            linkedCount = pwRecords.Count;
                        }
                        else
                        {
                            var privateWire = await _context.PrivateWires.FindAsync(id);
                            if (privateWire != null)
                            {
                                privateWire.EbillUserId = userId;
                                privateWire.IndexNumber = user.IndexNumber;
                                privateWire.ModifiedDate = modifiedDate;
                                privateWire.ModifiedBy = modifiedBy;
                                linkedCount = 1;
                            }
                        }
                        break;

                    case "safaricom":
                        // Link ALL unlinked Safaricom records with the same extension
                        if (!string.IsNullOrEmpty(extension))
                        {
                            var safRecords = await _context.Safaricoms
                                .Where(s => s.Ext == extension && s.EbillUserId == null)
                                .ToListAsync();

                            foreach (var saf in safRecords)
                            {
                                saf.EbillUserId = userId;
                                saf.IndexNumber = user.IndexNumber;
                                saf.ModifiedDate = modifiedDate;
                                saf.ModifiedBy = modifiedBy;
                            }
                            linkedCount = safRecords.Count;
                        }
                        else
                        {
                            var safaricom = await _context.Safaricoms.FindAsync(id);
                            if (safaricom != null)
                            {
                                safaricom.EbillUserId = userId;
                                safaricom.IndexNumber = user.IndexNumber;
                                safaricom.ModifiedDate = modifiedDate;
                                safaricom.ModifiedBy = modifiedBy;
                                linkedCount = 1;
                            }
                        }
                        break;

                    case "airtel":
                        // Link ALL unlinked Airtel records with the same extension
                        if (!string.IsNullOrEmpty(extension))
                        {
                            var airtelRecords = await _context.Airtels
                                .Where(a => a.Ext == extension && a.EbillUserId == null)
                                .ToListAsync();

                            foreach (var air in airtelRecords)
                            {
                                air.EbillUserId = userId;
                                air.IndexNumber = user.IndexNumber;
                                air.ModifiedDate = modifiedDate;
                                air.ModifiedBy = modifiedBy;
                            }
                            linkedCount = airtelRecords.Count;
                        }
                        else
                        {
                            var airtel = await _context.Airtels.FindAsync(id);
                            if (airtel != null)
                            {
                                airtel.EbillUserId = userId;
                                airtel.IndexNumber = user.IndexNumber;
                                airtel.ModifiedDate = modifiedDate;
                                airtel.ModifiedBy = modifiedBy;
                                linkedCount = 1;
                            }
                        }
                        break;

                    case "calllog":
                        // Link single CallLog record (legacy, no extension-based bulk link)
                        var callLog = await _context.CallLogs.FindAsync(id);
                        if (callLog != null)
                        {
                            callLog.EbillUserId = userId;
                            callLog.AccountNo = user.IndexNumber;
                            linkedCount = 1;
                        }
                        break;

                    default:
                        return new JsonResult(new { success = false, message = $"Unknown record type: {type}" });
                }

                if (linkedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Linked {Count} {Type} records with extension '{Extension}' to user {FirstName} {LastName} (ID: {UserId})",
                        linkedCount, type, extension ?? "N/A", user.FirstName, user.LastName, userId);

                    return new JsonResult(new
                    {
                        success = true,
                        message = $"Successfully linked {linkedCount} record(s) to {user.FirstName} {user.LastName}",
                        userName = $"{user.FirstName} {user.LastName}",
                        userIndex = user.IndexNumber,
                        linkedCount = linkedCount
                    });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "No records found to link" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking {Type} records with extension '{Extension}' to user {UserId}", type, extension, userId);
                return new JsonResult(new { success = false, message = "An error occurred while linking the records" });
            }
        }

        public async Task<IActionResult> OnPostLinkToRegisteredPhoneAsync(string type, int id, string extension, int userId)
        {
            try
            {
                // Find the registered phone in UserPhones
                var userPhone = await _context.UserPhones
                    .FirstOrDefaultAsync(up => up.PhoneNumber == extension && up.IsActive);

                if (userPhone == null)
                {
                    return new JsonResult(new { success = false, message = $"No active registered phone found for extension {extension}" });
                }

                bool updated = false;
                string recordInfo = "";

                switch (type?.ToLower())
                {
                    case "pstn":
                        var pstn = await _context.PSTNs.FindAsync(id);
                        if (pstn != null)
                        {
                            pstn.UserPhoneId = userPhone.Id;
                            pstn.ModifiedDate = DateTime.UtcNow;
                            pstn.ModifiedBy = User.Identity?.Name;
                            _context.Update(pstn);
                            updated = true;
                            recordInfo = $"PSTN #{id}";
                        }
                        break;

                    case "privatewire":
                    case "private wire":
                        var privateWire = await _context.PrivateWires.FindAsync(id);
                        if (privateWire != null)
                        {
                            privateWire.UserPhoneId = userPhone.Id;
                            privateWire.ModifiedDate = DateTime.UtcNow;
                            privateWire.ModifiedBy = User.Identity?.Name;
                            _context.Update(privateWire);
                            updated = true;
                            recordInfo = $"Private Wire #{id}";
                        }
                        break;

                    case "safaricom":
                        var safaricom = await _context.Safaricoms.FindAsync(id);
                        if (safaricom != null)
                        {
                            safaricom.UserPhoneId = userPhone.Id;
                            safaricom.ModifiedDate = DateTime.UtcNow;
                            safaricom.ModifiedBy = User.Identity?.Name;
                            _context.Update(safaricom);
                            updated = true;
                            recordInfo = $"Safaricom #{id}";
                        }
                        break;

                    case "airtel":
                        var airtel = await _context.Airtels.FindAsync(id);
                        if (airtel != null)
                        {
                            airtel.UserPhoneId = userPhone.Id;
                            airtel.ModifiedDate = DateTime.UtcNow;
                            airtel.ModifiedBy = User.Identity?.Name;
                            _context.Update(airtel);
                            updated = true;
                            recordInfo = $"Airtel #{id}";
                        }
                        break;

                    case "calllog":
                        var callLog = await _context.CallLogs.FindAsync(id);
                        if (callLog != null)
                        {
                            callLog.UserPhoneId = userPhone.Id;
                            _context.Update(callLog);
                            updated = true;
                            recordInfo = $"CallLog #{id}";
                        }
                        break;

                    default:
                        return new JsonResult(new { success = false, message = $"Unknown record type: {type}" });
                }

                if (updated)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Linked {RecordInfo} to registered phone {Extension} (UserPhoneId: {UserPhoneId})",
                        recordInfo, extension, userPhone.Id);

                    return new JsonResult(new
                    {
                        success = true,
                        message = $"Successfully linked to registered phone {extension}",
                        phoneInfo = $"{userPhone.PhoneType} - {userPhone.PhoneNumber}",
                        userPhoneId = userPhone.Id
                    });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "Record not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error linking {Type} record {Id} to registered phone {Extension}", type, id, extension);
                return new JsonResult(new { success = false, message = "An error occurred while linking to the registered phone" });
            }
        }

        public async Task<IActionResult> OnPostRegisterExtensionAsync(string type, int id, string extension, int userId, string phoneType, bool isPrimary, string? classOfService = null, string? location = null, string? notes = null, int lineType = 2)
        {
            try
            {
                // Sync LineType with IsPrimary - they must match
                LineType actualLineType = (LineType)lineType;
                if (actualLineType == LineType.Primary)
                {
                    isPrimary = true;
                }
                else
                {
                    // If LineType is not Primary, IsPrimary must be false
                    isPrimary = false;
                }

                var user = await _context.EbillUsers.FindAsync(userId);
                if (user == null)
                {
                    return new JsonResult(new { success = false, message = "User not found" });
                }

                // Check if extension already exists
                var existingPhone = await _context.UserPhones
                    .FirstOrDefaultAsync(up => up.PhoneNumber == extension);

                if (existingPhone != null)
                {
                    return new JsonResult(new { success = false, message = $"Extension {extension} is already registered" });
                }

                // If this is set as primary, unset other primary phones for this user and set their LineType to Secondary
                if (isPrimary)
                {
                    var userPhones = await _context.UserPhones
                        .Where(up => up.IndexNumber == user.IndexNumber && up.IsPrimary)
                        .ToListAsync();

                    foreach (var phone in userPhones)
                    {
                        phone.IsPrimary = false;
                        phone.LineType = LineType.Secondary;
                        _context.Update(phone);
                    }
                }

                // Look up ClassOfService by class if provided
                int? classOfServiceId = null;
                if (!string.IsNullOrEmpty(classOfService))
                {
                    var cos = await _context.ClassOfServices
                        .FirstOrDefaultAsync(c => c.Class == classOfService);
                    classOfServiceId = cos?.Id;
                }

                // Create new UserPhone record
                var newUserPhone = new UserPhone
                {
                    IndexNumber = user.IndexNumber,
                    PhoneNumber = extension,
                    PhoneType = phoneType,
                    LineType = actualLineType,
                    IsPrimary = isPrimary,
                    IsActive = true,
                    ClassOfServiceId = classOfServiceId,
                    Location = location,
                    Notes = notes,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name
                };

                _context.UserPhones.Add(newUserPhone);
                await _context.SaveChangesAsync();

                // Now link the call log record to this UserPhone
                bool updated = false;
                string recordInfo = "";

                switch (type?.ToLower())
                {
                    case "pstn":
                        var pstn = await _context.PSTNs.FindAsync(id);
                        if (pstn != null)
                        {
                            pstn.UserPhoneId = newUserPhone.Id;
                            pstn.ModifiedDate = DateTime.UtcNow;
                            pstn.ModifiedBy = User.Identity?.Name;
                            _context.Update(pstn);
                            updated = true;
                            recordInfo = $"PSTN #{id}";
                        }
                        break;

                    case "privatewire":
                    case "private wire":
                        var privateWire = await _context.PrivateWires.FindAsync(id);
                        if (privateWire != null)
                        {
                            privateWire.UserPhoneId = newUserPhone.Id;
                            privateWire.ModifiedDate = DateTime.UtcNow;
                            privateWire.ModifiedBy = User.Identity?.Name;
                            _context.Update(privateWire);
                            updated = true;
                            recordInfo = $"Private Wire #{id}";
                        }
                        break;

                    case "safaricom":
                        var safaricom = await _context.Safaricoms.FindAsync(id);
                        if (safaricom != null)
                        {
                            safaricom.UserPhoneId = newUserPhone.Id;
                            safaricom.ModifiedDate = DateTime.UtcNow;
                            safaricom.ModifiedBy = User.Identity?.Name;
                            _context.Update(safaricom);
                            updated = true;
                            recordInfo = $"Safaricom #{id}";
                        }
                        break;

                    case "airtel":
                        var airtel = await _context.Airtels.FindAsync(id);
                        if (airtel != null)
                        {
                            airtel.UserPhoneId = newUserPhone.Id;
                            airtel.ModifiedDate = DateTime.UtcNow;
                            airtel.ModifiedBy = User.Identity?.Name;
                            _context.Update(airtel);
                            updated = true;
                            recordInfo = $"Airtel #{id}";
                        }
                        break;

                    case "calllog":
                        var callLog = await _context.CallLogs.FindAsync(id);
                        if (callLog != null)
                        {
                            callLog.UserPhoneId = newUserPhone.Id;
                            _context.Update(callLog);
                            updated = true;
                            recordInfo = $"CallLog #{id}";
                        }
                        break;

                    default:
                        return new JsonResult(new { success = false, message = $"Unknown record type: {type}" });
                }

                if (updated)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Registered extension {Extension} as {PhoneType} for user {FirstName} {LastName} and linked {RecordInfo}",
                        extension, phoneType, user.FirstName, user.LastName, recordInfo);

                    return new JsonResult(new
                    {
                        success = true,
                        message = $"Successfully registered extension {extension} and linked to record",
                        extension = extension,
                        phoneType = phoneType,
                        userPhoneId = newUserPhone.Id
                    });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "Record not found after registration" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering extension {Extension} for user {UserId}", extension, userId);
                return new JsonResult(new { success = false, message = "An error occurred while registering the extension" });
            }
        }

        public async Task<IActionResult> OnGetUserForRegistrationAsync(int userId)
        {
            try
            {
                var user = await _context.EbillUsers
                    .Include(u => u.OrganizationEntity)
                    .Include(u => u.OfficeEntity)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return new JsonResult(new { success = false, message = "User not found" });
                }

                // Get existing phones for this user
                var phones = await _context.UserPhones
                    .Where(up => up.IndexNumber == user.IndexNumber)
                    .OrderByDescending(up => up.IsPrimary)
                    .ThenByDescending(up => up.IsActive)
                    .Select(up => new
                    {
                        up.PhoneNumber,
                        up.PhoneType,
                        up.IsPrimary,
                        up.IsActive,
                        ClassOfService = up.ClassOfService != null ? up.ClassOfService.Class : null,
                        up.Location,
                        up.Notes
                    })
                    .ToListAsync();

                return new JsonResult(new
                {
                    success = true,
                    user = new
                    {
                        user.FirstName,
                        user.LastName,
                        user.IndexNumber,
                        user.Email,
                        Organization = user.OrganizationEntity?.Name ?? "N/A",
                        Office = user.OfficeEntity?.Name ?? "N/A"
                    },
                    phones
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user information for registration for user {UserId}", userId);
                return new JsonResult(new { success = false, message = $"Error: {ex.Message}", details = ex.ToString() });
            }
        }

        public async Task<IActionResult> OnGetClassOfServicesAsync()
        {
            try
            {
                var classOfServices = await _context.ClassOfServices
                    .Where(cos => cos.ServiceStatus == ServiceStatus.Active)
                    .OrderBy(cos => cos.Class)
                    .Select(cos => new
                    {
                        cos.Class,
                        cos.Service,
                        cos.EligibleStaff
                    })
                    .ToListAsync();

                return new JsonResult(new
                {
                    success = true,
                    classOfServices
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Class of Services");
                return new JsonResult(new { success = false, message = $"Error: {ex.Message}", details = ex.ToString() });
            }
        }

        public async Task<IActionResult> OnPostDeleteTelecomAsync(string type, int id)
        {
            try
            {
                string recordInfo = "";
                bool isStaged = false;

                switch (type?.ToLower())
                {
                    case "pstn":
                        var pstn = await _context.PSTNs.FindAsync(id);
                        if (pstn != null)
                        {
                            if (pstn.StagingBatchId.HasValue)
                            {
                                isStaged = true;
                                recordInfo = $"PSTN #{id}";
                            }
                            else
                            {
                                _context.PSTNs.Remove(pstn);
                                recordInfo = $"PSTN #{id} ({pstn.IndexNumber})";
                            }
                        }
                        break;

                    case "privatewire":
                    case "private wire":
                        var privateWire = await _context.PrivateWires.FindAsync(id);
                        if (privateWire != null)
                        {
                            if (privateWire.StagingBatchId.HasValue)
                            {
                                isStaged = true;
                                recordInfo = $"Private Wire #{id}";
                            }
                            else
                            {
                                _context.PrivateWires.Remove(privateWire);
                                recordInfo = $"Private Wire #{id} ({privateWire.IndexNumber})";
                            }
                        }
                        break;

                    case "safaricom":
                        var safaricom = await _context.Safaricoms.FindAsync(id);
                        if (safaricom != null)
                        {
                            if (safaricom.StagingBatchId.HasValue)
                            {
                                isStaged = true;
                                recordInfo = $"Safaricom #{id}";
                            }
                            else
                            {
                                _context.Safaricoms.Remove(safaricom);
                                recordInfo = $"Safaricom #{id} ({safaricom.IndexNumber})";
                            }
                        }
                        break;

                    case "airtel":
                        var airtel = await _context.Airtels.FindAsync(id);
                        if (airtel != null)
                        {
                            if (airtel.StagingBatchId.HasValue)
                            {
                                isStaged = true;
                                recordInfo = $"Airtel #{id}";
                            }
                            else
                            {
                                _context.Airtels.Remove(airtel);
                                recordInfo = $"Airtel #{id} ({airtel.IndexNumber})";
                            }
                        }
                        break;

                    case "calllog":
                        var callLog = await _context.CallLogs.FindAsync(id);
                        if (callLog != null)
                        {
                            _context.CallLogs.Remove(callLog);
                            recordInfo = $"CallLog #{id} ({callLog.AccountNo})";
                        }
                        break;

                    default:
                        return new JsonResult(new { success = false, message = $"Unknown record type: {type}" });
                }

                if (isStaged)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = $"Cannot delete {recordInfo}. This record has been staged for verification and is part of the approval workflow. Please remove it from staging first."
                    });
                }

                if (!string.IsNullOrEmpty(recordInfo))
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Deleted {RecordInfo} by user {UserName}", recordInfo, User.Identity?.Name);

                    return new JsonResult(new
                    {
                        success = true,
                        message = $"Successfully deleted {recordInfo}"
                    });
                }
                else
                {
                    return new JsonResult(new { success = false, message = "Record not found" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting {Type} record {Id}", type, id);
                return new JsonResult(new { success = false, message = "An error occurred while deleting the record" });
            }
        }

        public async Task<IActionResult> OnGetCheckDeletePermissionAsync(string type, int id)
        {
            try
            {
                bool canDelete = true;
                int? importAuditId = null;

                switch (type?.ToLower())
                {
                    case "pstn":
                        var pstn = await _context.PSTNs.FindAsync(id);
                        if (pstn?.ImportAuditId.HasValue == true)
                        {
                            canDelete = false;
                            importAuditId = pstn.ImportAuditId;
                        }
                        break;

                    case "privatewire":
                    case "private wire":
                        var privateWire = await _context.PrivateWires.FindAsync(id);
                        if (privateWire?.ImportAuditId.HasValue == true)
                        {
                            canDelete = false;
                            importAuditId = privateWire.ImportAuditId;
                        }
                        break;

                    case "safaricom":
                        var safaricom = await _context.Safaricoms.FindAsync(id);
                        if (safaricom?.ImportAuditId.HasValue == true)
                        {
                            canDelete = false;
                            importAuditId = safaricom.ImportAuditId;
                        }
                        break;

                    case "airtel":
                        var airtel = await _context.Airtels.FindAsync(id);
                        if (airtel?.ImportAuditId.HasValue == true)
                        {
                            canDelete = false;
                            importAuditId = airtel.ImportAuditId;
                        }
                        break;

                    case "calllog":
                        // CallLogs can always be deleted
                        canDelete = true;
                        break;
                }

                return new JsonResult(new
                {
                    canDelete,
                    importAuditId,
                    message = canDelete ? "" : $"This record is part of import batch #{importAuditId} and cannot be deleted individually."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking delete permission for {Type} record {Id}", type, id);
                return new JsonResult(new { canDelete = false, message = "Error checking delete permission" });
            }
        }

        public async Task<IActionResult> OnGetCallLogDetailsAsync(int id)
        {
            var callLog = await _context.CallLogs
                .Include(c => c.EbillUser)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (callLog == null)
            {
                return NotFound();
            }

            var detailsHtml = $@"
                <style>
                    .detail-section {{
                        background: #ffffff;
                        border-radius: 8px;
                        padding: 20px;
                        margin-bottom: 16px;
                        border: 1px solid #e5e7eb;
                    }}
                    .section-title {{
                        font-size: 14px;
                        font-weight: 600;
                        color: #374151;
                        margin-bottom: 16px;
                        display: flex;
                        align-items: center;
                        gap: 8px;
                    }}
                    .info-row {{
                        display: flex;
                        justify-content: space-between;
                        align-items: start;
                        padding: 8px 0;
                        border-bottom: 1px solid #f3f4f6;
                    }}
                    .info-row:last-child {{
                        border-bottom: none;
                    }}
                    .info-label {{
                        font-size: 13px;
                        color: #6b7280;
                        font-weight: 500;
                    }}
                    .info-value {{
                        font-size: 14px;
                        color: #111827;
                        font-weight: 600;
                        text-align: right;
                    }}
                    .total-section {{
                        background: #f9fafb;
                        border-radius: 8px;
                        padding: 16px;
                        margin-top: 12px;
                        border: 1px solid #e5e7eb;
                    }}
                    .total-amount {{
                        font-size: 24px;
                        font-weight: 700;
                        color: #059669;
                    }}
                    .user-linked {{
                        background: #eff6ff;
                        border: 1px solid #3b82f6;
                        border-radius: 8px;
                        padding: 16px;
                    }}
                    .user-unlinked {{
                        background: #fef3c7;
                        border: 1px solid #f59e0b;
                        border-radius: 8px;
                        padding: 16px;
                        text-align: center;
                    }}
                    .meta-info {{
                        font-size: 12px;
                        color: #9ca3af;
                        text-align: center;
                        margin-top: 16px;
                        padding-top: 16px;
                        border-top: 1px solid #f3f4f6;
                    }}
                </style>

                <!-- Account Information -->
                <div class='row'>
                    <div class='col-md-6'>
                        <div class='detail-section'>
                            <h6 class='section-title'>
                                <i class='bi bi-building text-muted' style='font-size: 16px;'></i>
                                Account Information
                            </h6>
                            <div class='info-row'>
                                <span class='info-label'>Account Number</span>
                                <span class='info-value'>{callLog.AccountNo}</span>
                            </div>
                            <div class='info-row'>
                                <span class='info-label'>Sub Account</span>
                                <span class='info-value'>{callLog.SubAccountNo}</span>
                            </div>
                            <div class='info-row'>
                                <span class='info-label'>Account Name</span>
                                <span class='info-value' style='max-width: 200px; word-break: break-word;'>{callLog.SubAccountName}</span>
                            </div>
                        </div>
                    </div>
                    
                    <div class='col-md-6'>
                        <div class='detail-section'>
                            <h6 class='section-title'>
                                <i class='bi bi-telephone text-muted' style='font-size: 16px;'></i>
                                Contact & Invoice
                            </h6>
                            <div class='info-row'>
                                <span class='info-label'>Phone Number</span>
                                <span class='info-value'>{callLog.MSISDN}</span>
                            </div>
                            <div class='info-row'>
                                <span class='info-label'>Invoice Number</span>
                                <span class='info-value'>{(string.IsNullOrEmpty(callLog.InvoiceNo) ? "—" : callLog.InvoiceNo)}</span>
                            </div>
                            <div class='info-row'>
                                <span class='info-label'>Invoice Date</span>
                                <span class='info-value'>{callLog.InvoiceDate:MMM dd, yyyy}</span>
                            </div>
                        </div>
                    </div>
                </div>

                <!-- Billing Details -->
                <div class='detail-section'>
                    <h6 class='section-title'>
                        <i class='bi bi-receipt text-muted' style='font-size: 16px;'></i>
                        Billing Details
                    </h6>
                    <div class='info-row'>
                        <span class='info-label'>Net Access Fee</span>
                        <span class='info-value'>{callLog.NetAccessFee:C}</span>
                    </div>
                    <div class='info-row'>
                        <span class='info-label'>Net Usage (Less Tax)</span>
                        <span class='info-value'>{callLog.NetUsageLessTax:C}</span>
                    </div>
                    <div class='info-row'>
                        <span class='info-label'>Less Taxes</span>
                        <span class='info-value'>{callLog.LessTaxes:C}</span>
                    </div>
                    {(callLog.VAT16.HasValue ? $@"
                    <div class='info-row'>
                        <span class='info-label'>VAT (16%)</span>
                        <span class='info-value'>{callLog.VAT16.Value:C}</span>
                    </div>" : "")}
                    {(callLog.Excise15.HasValue ? $@"
                    <div class='info-row'>
                        <span class='info-label'>Excise (15%)</span>
                        <span class='info-value'>{callLog.Excise15.Value:C}</span>
                    </div>" : "")}
                    
                    <div class='total-section'>
                        <div class='d-flex justify-content-between align-items-center'>
                            <span class='text-muted fw-semibold'>Total Amount</span>
                            <span class='total-amount'>{callLog.GrossTotal:C}</span>
                        </div>
                    </div>
                </div>

                <!-- User Link Status -->
                {(callLog.EbillUser != null ? $@"
                <div class='user-linked'>
                    <h6 class='section-title mb-3'>
                        <i class='bi bi-person-check text-primary' style='font-size: 16px;'></i>
                        Linked E-bill User
                    </h6>
                    <div class='row'>
                        <div class='col-md-4'>
                            <small class='text-muted d-block'>Name</small>
                            <span class='fw-semibold'>{callLog.EbillUser.FirstName} {callLog.EbillUser.LastName}</span>
                        </div>
                        <div class='col-md-4'>
                            <small class='text-muted d-block'>Index Number</small>
                            <span class='fw-semibold'>{callLog.EbillUser.IndexNumber}</span>
                        </div>
                        <div class='col-md-4'>
                            <small class='text-muted d-block'>Email</small>
                            <span class='fw-semibold text-truncate d-block'>{callLog.EbillUser.Email}</span>
                        </div>
                    </div>
                </div>" : @"
                <div class='user-unlinked'>
                    <i class='bi bi-exclamation-triangle text-warning mb-2' style='font-size: 24px;'></i>
                    <h6 class='mb-1'>Not Linked to User</h6>
                    <small class='text-muted'>This call log is not associated with any E-bill user</small>
                </div>")}

                <!-- Import Metadata -->
                <div class='meta-info'>
                    <i class='bi bi-info-circle me-1'></i>
                    Imported by <strong>{(string.IsNullOrEmpty(callLog.ImportedBy) ? "System" : callLog.ImportedBy)}</strong>
                    on <strong>{(callLog.ImportedDate.HasValue ? callLog.ImportedDate.Value.ToString("MMM dd, yyyy hh:mm tt") : "N/A")}</strong>
                </div>";

            return Content(detailsHtml, "text/html");
        }

        public async Task<IActionResult> OnPostImportCallLogsAsync(IFormFile callLogFile, string callLogType = "regular", bool updateExisting = false, bool skipUnmatched = true, int? billingMonth = null, int? billingYear = null, string? dateFormat = null)
        {
            var startTime = DateTime.UtcNow;

            if (callLogFile == null || callLogFile.Length == 0)
            {
                StatusMessage = "Please select a CSV file to import.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            if (callLogFile.Length > 10 * 1024 * 1024) // 10MB limit for call logs
            {
                StatusMessage = "File size exceeds 10MB limit.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            // Validate billing period
            if (!billingMonth.HasValue || !billingYear.HasValue || billingMonth < 1 || billingMonth > 12 || billingYear < 2000)
            {
                StatusMessage = "Please select a valid billing month and year.";
                StatusMessageClass = "danger";
                return RedirectToPage();
            }

            // Validate date format
            if (string.IsNullOrWhiteSpace(dateFormat))
            {
                dateFormat = "dd/MM/yyyy"; // Default to DD/MM/YYYY
            }

            // Calculate billing period (first day of the month)
            var billingPeriodDate = new DateTime(billingYear.Value, billingMonth.Value, 1);
            var billingPeriodString = billingPeriodDate.ToString("yyyy-MM-dd");

            var importResults = new List<string>();
            var detailedResult = new ImportDetailedResult();
            var successCount = 0;
            var skipCount = 0;
            var errorCount = 0;
            var updateCount = 0;
            var unmatchedCount = 0;
            var totalRecords = 0;
            var csvHeaders = Array.Empty<string>(); // Store headers for better data parsing

            try
            {
                using var reader = new StreamReader(callLogFile.OpenReadStream());
                var headerLine = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(headerLine))
                {
                    StatusMessage = "CSV file is empty or invalid.";
                    StatusMessageClass = "danger";
                    return RedirectToPage();
                }

                var headers = ParseCsvLine(headerLine);
                csvHeaders = headers; // Store for later use

                // Handle PSTN imports separately
                if (callLogType == "PSTN")
                {
                    return await ImportPSTNDataAsync(reader, headers, headerLine, updateExisting, billingMonth.Value, billingYear.Value, billingPeriodString, dateFormat);
                }

                // Handle Private Wire imports separately
                if (callLogType == "PrivateWire")
                {
                    return await ImportPrivateWireDataAsync(reader, headers, headerLine, updateExisting, billingMonth.Value, billingYear.Value, billingPeriodString, dateFormat);
                }

                // Handle Safaricom imports separately
                if (callLogType == "Safaricom")
                {
                    return await ImportSafaricomDataAsync(reader, headers, headerLine, updateExisting, billingMonth.Value, billingYear.Value, billingPeriodString, dateFormat);
                }

                // Handle Airtel imports separately
                if (callLogType == "Airtel")
                {
                    return await ImportAirtelDataAsync(reader, headers, headerLine, updateExisting, billingMonth.Value, billingYear.Value, billingPeriodString, dateFormat);
                }

                var requiredColumns = new[] {
                    "AccountNo", "SubAccountNo", "SubAccountName", "MSISDN",
                    "InvoiceDate", "NetAccessFee", "NetUsageLessTax", "LessTaxes", "GrossTotal"
                };

                // Validate required columns
                foreach (var required in requiredColumns)
                {
                    if (!headers.Any(h => h.Trim().Equals(required, StringComparison.OrdinalIgnoreCase)))
                    {
                        StatusMessage = $"Required column '{required}' not found in CSV file.";
                        StatusMessageClass = "danger";
                        return RedirectToPage();
                    }
                }

                // Create column index mapping
                var columnIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < headers.Length; i++)
                {
                    columnIndices[headers[i].Trim()] = i;
                }

                var lineNumber = 1;
                var originalLines = new Dictionary<int, string>(); // Store original CSV lines for error reporting
                
                while (!reader.EndOfStream)
                {
                    lineNumber++;
                    var line = await reader.ReadLineAsync();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Parse values first to check if row is truly empty
                    var values = ParseCsvLine(line);

                    // Skip rows where all values are empty or whitespace
                    if (values.All(v => string.IsNullOrWhiteSpace(v)))
                        continue;

                    totalRecords++;
                    originalLines[lineNumber] = line;

                    try
                    {
                        if (values.Length != headers.Length)
                        {
                            importResults.Add($"Line {lineNumber}: Invalid number of columns");
                            errorCount++;
                            detailedResult.Errors.Add(new ImportErrorDetail
                            {
                                LineNumber = lineNumber,
                                OriginalData = line,
                                ErrorMessage = $"Expected {headers.Length} columns, found {values.Length}",
                                FieldName = "Column Count"
                            });
                            continue;
                        }

                        // Extract values
                        var msisdn = values[columnIndices["MSISDN"]].Trim();
                        var invoiceNo = columnIndices.ContainsKey("InvoiceNo") ? values[columnIndices["InvoiceNo"]].Trim() : "";
                        
                        // Find matching EbillUser by phone number
                        var ebillUser = await _context.EbillUsers
                            .FirstOrDefaultAsync(u => u.OfficialMobileNumber == msisdn);
                        
                        if (ebillUser == null && skipUnmatched)
                        {
                            importResults.Add($"Line {lineNumber}: No E-bill user found with phone number {msisdn}");
                            unmatchedCount++;
                            detailedResult.Skipped.Add(new ImportSkippedDetail
                            {
                                LineNumber = lineNumber,
                                OriginalData = line,
                                Reason = "No matching E-bill user found",
                                LookupValue = msisdn
                            });
                            continue;
                        }

                        // Check if call log already exists for this invoice
                        CallLog? existingCallLog = null;
                        if (!string.IsNullOrEmpty(invoiceNo))
                        {
                            existingCallLog = await _context.CallLogs
                                .FirstOrDefaultAsync(c => c.InvoiceNo == invoiceNo);
                        }

                        if (existingCallLog != null && !updateExisting)
                        {
                            importResults.Add($"Line {lineNumber}: Call log for invoice {invoiceNo} already exists");
                            skipCount++;
                            detailedResult.Skipped.Add(new ImportSkippedDetail
                            {
                                LineNumber = lineNumber,
                                OriginalData = line,
                                Reason = "Duplicate invoice number",
                                LookupValue = invoiceNo
                            });
                            continue;
                        }

                        // Parse dates
                        if (!DateTime.TryParse(values[columnIndices["InvoiceDate"]], out var invoiceDate))
                        {
                            importResults.Add($"Line {lineNumber}: Invalid invoice date format");
                            errorCount++;
                            detailedResult.Errors.Add(new ImportErrorDetail
                            {
                                LineNumber = lineNumber,
                                OriginalData = line,
                                ErrorMessage = $"Invalid date format: '{values[columnIndices["InvoiceDate"]]}'.",
                                FieldName = "InvoiceDate"
                            });
                            continue;
                        }

                        // Parse decimal values
                        if (!decimal.TryParse(values[columnIndices["NetAccessFee"]], out var netAccessFee) ||
                            !decimal.TryParse(values[columnIndices["NetUsageLessTax"]], out var netUsageLessTax) ||
                            !decimal.TryParse(values[columnIndices["LessTaxes"]], out var lessTaxes) ||
                            !decimal.TryParse(values[columnIndices["GrossTotal"]], out var grossTotal))
                        {
                            importResults.Add($"Line {lineNumber}: Invalid numeric values");
                            errorCount++;
                            var invalidFields = new List<string>();
                            if (!decimal.TryParse(values[columnIndices["NetAccessFee"]], out _)) invalidFields.Add("NetAccessFee");
                            if (!decimal.TryParse(values[columnIndices["NetUsageLessTax"]], out _)) invalidFields.Add("NetUsageLessTax");
                            if (!decimal.TryParse(values[columnIndices["LessTaxes"]], out _)) invalidFields.Add("LessTaxes");
                            if (!decimal.TryParse(values[columnIndices["GrossTotal"]], out _)) invalidFields.Add("GrossTotal");
                            
                            detailedResult.Errors.Add(new ImportErrorDetail
                            {
                                LineNumber = lineNumber,
                                OriginalData = line,
                                ErrorMessage = $"Invalid numeric format in fields: {string.Join(", ", invalidFields)}",
                                FieldName = string.Join(", ", invalidFields)
                            });
                            continue;
                        }

                        // Parse optional decimal values
                        decimal? vat16 = null;
                        decimal? excise15 = null;
                        
                        if (columnIndices.ContainsKey("VAT16"))
                        {
                            var vatValue = values[columnIndices["VAT16"]].Trim();
                            if (!string.IsNullOrEmpty(vatValue) && vatValue != "-" && vatValue != "–")
                            {
                                if (decimal.TryParse(vatValue, out var parsedVat))
                                    vat16 = parsedVat;
                            }
                        }
                        
                        if (columnIndices.ContainsKey("Excise15"))
                        {
                            var exciseValue = values[columnIndices["Excise15"]].Trim();
                            if (!string.IsNullOrEmpty(exciseValue) && exciseValue != "-" && exciseValue != "–")
                            {
                                if (decimal.TryParse(exciseValue, out var parsedExcise))
                                    excise15 = parsedExcise;
                            }
                        }

                        if (existingCallLog != null && updateExisting)
                        {
                            // Update existing call log
                            existingCallLog.AccountNo = values[columnIndices["AccountNo"]].Trim();
                            existingCallLog.SubAccountNo = values[columnIndices["SubAccountNo"]].Trim();
                            existingCallLog.SubAccountName = values[columnIndices["SubAccountName"]].Trim();
                            existingCallLog.MSISDN = msisdn;
                            existingCallLog.TaxInvoiceSummaryNo = columnIndices.ContainsKey("TaxInvoiceSummaryNo") ? 
                                values[columnIndices["TaxInvoiceSummaryNo"]].Trim() : "";
                            existingCallLog.InvoiceDate = invoiceDate;
                            existingCallLog.NetAccessFee = netAccessFee;
                            existingCallLog.NetUsageLessTax = netUsageLessTax;
                            existingCallLog.LessTaxes = lessTaxes;
                            existingCallLog.VAT16 = vat16;
                            existingCallLog.Excise15 = excise15;
                            existingCallLog.GrossTotal = grossTotal;
                            existingCallLog.EbillUserId = ebillUser?.Id;
                            existingCallLog.ImportedBy = User.Identity?.Name;
                            existingCallLog.ImportedDate = DateTime.UtcNow;
                            
                            updateCount++;
                            detailedResult.Updated.Add(new ImportUpdatedDetail
                            {
                                LineNumber = lineNumber,
                                RecordId = existingCallLog.Id,
                                Summary = $"Invoice {invoiceNo} - {msisdn}"
                            });
                        }
                        else
                        {
                            // Create new call log
                            var callLog = new CallLog
                            {
                                AccountNo = values[columnIndices["AccountNo"]].Trim(),
                                SubAccountNo = values[columnIndices["SubAccountNo"]].Trim(),
                                SubAccountName = values[columnIndices["SubAccountName"]].Trim(),
                                MSISDN = msisdn,
                                TaxInvoiceSummaryNo = columnIndices.ContainsKey("TaxInvoiceSummaryNo") ? 
                                    values[columnIndices["TaxInvoiceSummaryNo"]].Trim() : "",
                                InvoiceNo = invoiceNo,
                                InvoiceDate = invoiceDate,
                                NetAccessFee = netAccessFee,
                                NetUsageLessTax = netUsageLessTax,
                                LessTaxes = lessTaxes,
                                VAT16 = vat16,
                                Excise15 = excise15,
                                GrossTotal = grossTotal,
                                EbillUserId = ebillUser?.Id,
                                CreatedDate = DateTime.UtcNow,
                                ImportedBy = User.Identity?.Name,
                                ImportedDate = DateTime.UtcNow
                            };

                            _context.CallLogs.Add(callLog);
                            successCount++;
                            
                            // We'll add the success record after SaveChanges to get the ID
                        }
                    }
                    catch (Exception ex)
                    {
                        importResults.Add($"Line {lineNumber}: {ex.Message}");
                        errorCount++;
                        detailedResult.Errors.Add(new ImportErrorDetail
                        {
                            LineNumber = lineNumber,
                            OriginalData = originalLines.ContainsKey(lineNumber) ? originalLines[lineNumber] : "Data not available",
                            ErrorMessage = ex.Message
                        });
                    }
                }

                await _context.SaveChangesAsync();

                // Create audit record
                var processingTime = DateTime.UtcNow - startTime;
                var importAudit = new ImportAudit
                {
                    ImportType = "CallLogs",
                    FileName = callLogFile.FileName,
                    FileSize = callLogFile.Length,
                    TotalRecords = totalRecords,
                    SuccessCount = successCount,
                    UpdatedCount = updateCount,
                    SkippedCount = skipCount + unmatchedCount,
                    ErrorCount = errorCount,
                    ImportDate = DateTime.UtcNow,
                    ImportedBy = User.Identity?.Name ?? "Unknown",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    ProcessingTime = processingTime,
                    DetailedResults = JsonSerializer.Serialize(detailedResult),
                    ImportOptions = JsonSerializer.Serialize(new { updateExisting, skipUnmatched, headers = csvHeaders }),
                    DateFormatPreferences = JsonSerializer.Serialize(new
                    {
                        callLogType = callLogType,
                        dateFormat = dateFormat,
                        savedAt = DateTime.UtcNow
                    })
                };

                // Prepare import summary
                var summary = new List<string>
                {
                    $"Import completed: {successCount} new call logs created"
                };
                
                if (updateCount > 0)
                    summary.Add($"{updateCount} existing call logs updated");
                if (skipCount > 0)
                    summary.Add($"{skipCount} duplicates skipped");
                if (unmatchedCount > 0)
                    summary.Add($"{unmatchedCount} unmatched phone numbers skipped");
                if (errorCount > 0)
                    summary.Add($"{errorCount} errors encountered");

                StatusMessage = string.Join(", ", summary);
                StatusMessageClass = errorCount > 0 ? "warning" : "success";
                
                importAudit.SummaryMessage = StatusMessage;
                _context.ImportAudits.Add(importAudit);
                await _context.SaveChangesAsync();

                if (importResults.Any())
                {
                    TempData["ImportResults"] = JsonSerializer.Serialize(importResults.Take(100)); // Limit to first 100 errors
                    TempData["ImportAuditId"] = importAudit.Id; // Store audit ID for download
                }

                _logger.LogInformation("Call log import completed: {SuccessCount} created, {UpdateCount} updated, {SkipCount} skipped, {UnmatchedCount} unmatched, {ErrorCount} errors", 
                    successCount, updateCount, skipCount, unmatchedCount, errorCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing call log CSV file");
                StatusMessage = $"Import failed: {ex.Message}";
                StatusMessageClass = "danger";
                
                // Still create an audit record for failed imports
                var failedAudit = new ImportAudit
                {
                    ImportType = "CallLogs",
                    FileName = callLogFile.FileName,
                    FileSize = callLogFile.Length,
                    TotalRecords = 0,
                    SuccessCount = 0,
                    UpdatedCount = 0,
                    SkippedCount = 0,
                    ErrorCount = 1,
                    ImportDate = DateTime.UtcNow,
                    ImportedBy = User.Identity?.Name ?? "Unknown",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    ProcessingTime = DateTime.UtcNow - startTime,
                    SummaryMessage = StatusMessage,
                    DetailedResults = JsonSerializer.Serialize(new ImportDetailedResult 
                    { 
                        Errors = new List<ImportErrorDetail> 
                        { 
                            new ImportErrorDetail 
                            { 
                                LineNumber = 0, 
                                ErrorMessage = ex.Message 
                            } 
                        } 
                    })
                };
                _context.ImportAudits.Add(failedAudit);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetDownloadImportResultsAsync(int auditId, string type)
        {
            var audit = await _context.ImportAudits.FindAsync(auditId);
            if (audit == null || audit.ImportedBy != User.Identity?.Name)
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(audit.DetailedResults))
            {
                return NotFound();
            }

            var detailedResults = JsonSerializer.Deserialize<ImportDetailedResult>(audit.DetailedResults);
            if (detailedResults == null)
            {
                return NotFound();
            }

            // Try to get headers from import options
            string[]? csvHeaders = null;
            if (!string.IsNullOrEmpty(audit.ImportOptions))
            {
                try
                {
                    var options = JsonSerializer.Deserialize<JsonElement>(audit.ImportOptions);
                    if (options.TryGetProperty("headers", out var headersElement))
                    {
                        csvHeaders = JsonSerializer.Deserialize<string[]>(headersElement.GetRawText());
                    }
                }
                catch { }
            }

            var csv = new StringBuilder();
            var fileName = "";

            switch (type?.ToLower())
            {
                case "errors":
                    fileName = $"CallLog_Import_Errors_{audit.ImportDate:yyyyMMdd_HHmmss}.csv";
                    
                    // For errors, we'll create a more detailed format
                    csv.AppendLine("Line Number,Error Message,Field Name,Account No,Sub Account No,Sub Account Name,MSISDN,Invoice No,Invoice Date,Gross Total");
                    
                    foreach (var error in detailedResults.Errors)
                    {
                        var parsedData = ParseOriginalDataForDisplay(error.OriginalData);
                        csv.AppendLine($"{error.LineNumber}," +
                                     $"\"{error.ErrorMessage?.Replace("\"", "\"\"")}\"," +
                                     $"\"{error.FieldName?.Replace("\"", "\"\"")}\"," +
                                     $"\"{parsedData.GetValueOrDefault("AccountNo", "")}\"," +
                                     $"\"{parsedData.GetValueOrDefault("SubAccountNo", "")}\"," +
                                     $"\"{parsedData.GetValueOrDefault("SubAccountName", "")}\"," +
                                     $"\"{parsedData.GetValueOrDefault("MSISDN", "")}\"," +
                                     $"\"{parsedData.GetValueOrDefault("InvoiceNo", "")}\"," +
                                     $"\"{parsedData.GetValueOrDefault("InvoiceDate", "")}\"," +
                                     $"\"{parsedData.GetValueOrDefault("GrossTotal", "")}\"");
                    }
                    break;

                case "skipped":
                    fileName = $"CallLog_Import_Skipped_{audit.ImportDate:yyyyMMdd_HHmmss}.csv";
                    
                    // For skipped records, show key fields to help identify the records
                    csv.AppendLine("Line Number,Reason,Phone Number (MSISDN),Account No,Sub Account Name,Invoice No,Invoice Date,Gross Total");
                    
                    foreach (var skipped in detailedResults.Skipped)
                    {
                        var parsedData = ParseOriginalDataForDisplay(skipped.OriginalData);
                        csv.AppendLine($"{skipped.LineNumber}," +
                                     $"\"{skipped.Reason?.Replace("\"", "\"\"")}\"," +
                                     $"\"{skipped.LookupValue?.Replace("\"", "\"\"")}\"," +
                                     $"\"{parsedData.GetValueOrDefault("AccountNo", "")}\"," +
                                     $"\"{parsedData.GetValueOrDefault("SubAccountName", "")}\"," +
                                     $"\"{parsedData.GetValueOrDefault("InvoiceNo", "")}\"," +
                                     $"\"{parsedData.GetValueOrDefault("InvoiceDate", "")}\"," +
                                     $"\"{parsedData.GetValueOrDefault("GrossTotal", "")}\"");
                    }
                    break;

                case "all":
                    fileName = $"CallLog_Import_Complete_Results_{audit.ImportDate:yyyyMMdd_HHmmss}.csv";
                    csv.AppendLine("Status,Line Number,Message/Reason,Account No,Sub Account Name,MSISDN,Invoice No,Invoice Date,Gross Total");
                    
                    // Sort all results by line number for easier review
                    var allResults = new List<(string Status, int LineNumber, string Message, Dictionary<string, string> Data)>();
                    
                    foreach (var success in detailedResults.Successes)
                    {
                        allResults.Add(("Success", success.LineNumber, success.Summary ?? "Imported successfully", new Dictionary<string, string>()));
                    }
                    
                    foreach (var updated in detailedResults.Updated)
                    {
                        allResults.Add(("Updated", updated.LineNumber, updated.Summary ?? "Updated existing record", new Dictionary<string, string>()));
                    }
                    
                    foreach (var error in detailedResults.Errors)
                    {
                        var data = ParseOriginalDataForDisplay(error.OriginalData);
                        allResults.Add(("Error", error.LineNumber, error.ErrorMessage ?? "Unknown error", data));
                    }
                    
                    foreach (var skipped in detailedResults.Skipped)
                    {
                        var data = ParseOriginalDataForDisplay(skipped.OriginalData);
                        allResults.Add(("Skipped", skipped.LineNumber, skipped.Reason ?? "Unknown reason", data));
                    }
                    
                    // Sort by line number and output
                    foreach (var result in allResults.OrderBy(r => r.LineNumber))
                    {
                        csv.AppendLine($"{result.Status},{result.LineNumber}," +
                                     $"\"{result.Message.Replace("\"", "\"\"")}\"," +
                                     $"\"{result.Data.GetValueOrDefault("AccountNo", "")}\"," +
                                     $"\"{result.Data.GetValueOrDefault("SubAccountName", "")}\"," +
                                     $"\"{result.Data.GetValueOrDefault("MSISDN", "")}\"," +
                                     $"\"{result.Data.GetValueOrDefault("InvoiceNo", "")}\"," +
                                     $"\"{result.Data.GetValueOrDefault("InvoiceDate", "")}\"," +
                                     $"\"{result.Data.GetValueOrDefault("GrossTotal", "")}\"");
                    }
                    break;

                default:
                    return BadRequest();
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", fileName);
        }

        private Dictionary<string, string> ParseOriginalDataForDisplay(string? originalData)
        {
            var result = new Dictionary<string, string>();
            
            if (string.IsNullOrEmpty(originalData))
                return result;
            
            try
            {
                // Parse the CSV line
                var values = ParseCsvLine(originalData);
                
                // Try to get headers from the current import context (if available)
                // Otherwise use standard column mapping
                var standardColumns = new[] { 
                    "AccountNo", "SubAccountNo", "SubAccountName", "MSISDN", 
                    "TaxInvoiceSummaryNo", "InvoiceNo", "InvoiceDate", 
                    "NetAccessFee", "NetUsageLessTax", "LessTaxes", 
                    "VAT16", "Excise15", "GrossTotal" 
                };
                
                // Map values to column names (up to the number of values we have)
                for (int i = 0; i < Math.Min(values.Length, standardColumns.Length); i++)
                {
                    if (i < standardColumns.Length)
                    {
                        result[standardColumns[i]] = values[i].Trim();
                    }
                }
                
                // Clean up common formatting issues
                if (result.ContainsKey("InvoiceDate") && DateTime.TryParse(result["InvoiceDate"], out var date))
                {
                    result["InvoiceDate"] = date.ToString("yyyy-MM-dd");
                }
                
                // Format currency values
                foreach (var key in new[] { "NetAccessFee", "NetUsageLessTax", "LessTaxes", "VAT16", "Excise15", "GrossTotal" })
                {
                    if (result.ContainsKey(key) && decimal.TryParse(result[key], out var amount))
                    {
                        result[key] = amount.ToString("F2");
                    }
                }
            }
            catch
            {
                // If parsing fails, return empty dictionary
            }
            
            return result;
        }

        private async Task<IActionResult> ImportPSTNDataAsync(StreamReader reader, string[] headers, string headerLine, bool updateExisting, int billingMonth, int billingYear, string billingPeriodString, string dateFormat)
        {
            var startTime = DateTime.UtcNow;
            var importResults = new List<string>();
            var successCount = 0;
            var errorCount = 0;
            var updateCount = 0;
            var totalRecords = 0;
            var detailedResult = new ImportDetailedResult();

            try
            {
                // Validate PSTN required columns - updated for normalized structure
                var requiredPSTNColumns = new[] { "inde_", "dialed", "date", "kshs" };

                foreach (var required in requiredPSTNColumns)
                {
                    if (!headers.Any(h => h.Trim().Equals(required, StringComparison.OrdinalIgnoreCase)))
                    {
                        StatusMessage = $"Required PSTN column '{required}' not found in CSV file. Found columns: {string.Join(", ", headers)}";
                        StatusMessageClass = "danger";
                        return RedirectToPage();
                    }
                }

                // Create column index mapping
                var columnIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < headers.Length; i++)
                {
                    columnIndices[headers[i].Trim()] = i;
                }

                var lineNumber = 1;
                var originalLines = new Dictionary<int, string>();

                while (!reader.EndOfStream)
                {
                    lineNumber++;
                    var line = await reader.ReadLineAsync();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Parse values first to check if row is truly empty
                    var values = ParseCsvLine(line);

                    // Skip rows where all values are empty or whitespace
                    if (values.All(v => string.IsNullOrWhiteSpace(v)))
                        continue;

                    totalRecords++;
                    originalLines[lineNumber] = line;

                    try
                    {
                        if (values.Length != headers.Length)
                        {
                            importResults.Add($"Line {lineNumber}: Invalid number of columns");
                            errorCount++;
                            detailedResult.Errors.Add(new ImportErrorDetail
                            {
                                LineNumber = lineNumber,
                                OriginalData = line,
                                ErrorMessage = $"Expected {headers.Length} columns, found {values.Length}",
                                FieldName = "Column Count"
                            });
                            continue;
                        }

                        // Parse date
                        // Parse date using flexible parser
                        var dateParse = ParseDateField(values[columnIndices["date"]], dateFormat, lineNumber, "date");
                        if (!dateParse.Success)
                        {
                            importResults.Add($"Line {lineNumber}: {dateParse.ErrorMessage}");
                            errorCount++;
                            detailedResult.Errors.Add(new ImportErrorDetail
                            {
                                LineNumber = lineNumber,
                                OriginalData = line,
                                ErrorMessage = dateParse.ErrorMessage,
                                FieldName = "date"
                            });
                            continue;
                        }
                        var date = dateParse.Date!.Value;
                        // Parse amount
                        if (!decimal.TryParse(values[columnIndices["kshs"]], out var kshs))
                        {
                            importResults.Add($"Line {lineNumber}: Invalid amount format");
                            errorCount++;
                            detailedResult.Errors.Add(new ImportErrorDetail
                            {
                                LineNumber = lineNumber,
                                OriginalData = line,
                                ErrorMessage = $"Invalid amount format: '{values[columnIndices["kshs"]]}'",
                                FieldName = "kshs"
                            });
                            continue;
                        }

                        // Parse optional time field
                        TimeSpan? time = null;
                        if (columnIndices.ContainsKey("time") && !string.IsNullOrEmpty(values[columnIndices["time"]]))
                        {
                            TimeSpan.TryParse(values[columnIndices["time"]], out var parsedTime);
                            time = parsedTime;
                        }

                        // Parse optional duration fields
                        decimal? dur = null, durx = null;
                        if (columnIndices.ContainsKey("dur") && !string.IsNullOrEmpty(values[columnIndices["dur"]]))
                        {
                            decimal.TryParse(values[columnIndices["dur"]], out var parsedDur);
                            dur = parsedDur;
                        }
                        if (columnIndices.ContainsKey("durx") && !string.IsNullOrEmpty(values[columnIndices["durx"]]))
                        {
                            decimal.TryParse(values[columnIndices["durx"]], out var parsedDurx);
                            durx = parsedDurx;
                        }

                        // Try to match with EbillUser by IndexNumber
                        var dialedNumber = values[columnIndices["dialed"]];
                        var indexNumber = columnIndices.ContainsKey("inde_") ? values[columnIndices["inde_"]].Trim() : null;
                        var extension = columnIndices.ContainsKey("ext") ? values[columnIndices["ext"]].Trim() : null;

                        // First try to lookup by Extension in UserPhones table
                        UserPhone? userPhone = null;
                        EbillUser? ebillUser = null;

                        if (!string.IsNullOrEmpty(extension))
                        {
                            userPhone = await _context.UserPhones
                                .Include(up => up.EbillUser)
                                .FirstOrDefaultAsync(up => up.PhoneNumber == extension && up.IsActive);

                            if (userPhone != null)
                            {
                                // Get user info from UserPhone relationship
                                ebillUser = userPhone.EbillUser;
                                indexNumber = userPhone.IndexNumber; // Override with UserPhone's IndexNumber
                            }
                        }

                        // Fallback: If no UserPhone found but IndexNumber exists in CSV, try to match EbillUser directly
                        if (ebillUser == null && !string.IsNullOrEmpty(indexNumber))
                        {
                            ebillUser = await _context.EbillUsers
                                .FirstOrDefaultAsync(e => e.IndexNumber == indexNumber);
                        }

                        // Calculate USD amount
                        var amountUSD = await CalculateUSDAmount(kshs, billingMonth, billingYear);

                        var pstn = new PSTN
                        {
                            Extension = extension,
                            DialedNumber = dialedNumber,
                            CallTime = time,
                            Destination = columnIndices.ContainsKey("dest") ? values[columnIndices["dest"]] : null,
                            DestinationLine = columnIndices.ContainsKey("dl") ? values[columnIndices["dl"]] : null,
                            DurationExtended = durx,
                            // Organization info obtained through EbillUserId relationship
                            CallDate = date,
                            Duration = dur,
                            AmountKSH = kshs,
                            AmountUSD = amountUSD,
                            IndexNumber = indexNumber,
                            BillingPeriod = billingPeriodString,  // Use provided billing period
                            CallMonth = billingMonth,  // Use provided billing month
                            CallYear = billingYear,    // Use provided billing year
                            Carrier = columnIndices.ContainsKey("car") ? values[columnIndices["car"]] : null,
                            UserPhoneId = userPhone?.Id,  // Link to UserPhone if found by extension
                            EbillUserId = ebillUser?.Id,  // Link to EbillUser if found
                            CreatedDate = DateTime.UtcNow,
                            CreatedBy = User.Identity?.Name ?? "Unknown"
                        };

                        _context.PSTNs.Add(pstn);
                        successCount++;

                        detailedResult.Successes.Add(new ImportSuccessDetail
                        {
                            LineNumber = lineNumber,
                            RecordId = 0, // Will be updated after save
                            Summary = $"PSTN record - {values[columnIndices["dialed"]]}"
                        });
                    }
                    catch (Exception lineEx)
                    {
                        importResults.Add($"Line {lineNumber}: {lineEx.Message}");
                        errorCount++;
                        detailedResult.Errors.Add(new ImportErrorDetail
                        {
                            LineNumber = lineNumber,
                            OriginalData = line,
                            ErrorMessage = lineEx.Message,
                            FieldName = "General"
                        });
                    }
                }

                // Save all PSTN records first
                await _context.SaveChangesAsync();

                // Create import audit
                var audit = new ImportAudit
                {
                    ImportType = "PSTN",
                    FileName = headerLine,
                    FileSize = 0, // We don't have the original file size here
                    TotalRecords = totalRecords,
                    SuccessCount = successCount,
                    UpdatedCount = updateCount,
                    SkippedCount = 0,
                    ErrorCount = errorCount,
                    ImportDate = DateTime.UtcNow,
                    ImportedBy = User.Identity?.Name ?? "Unknown",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    ProcessingTime = DateTime.UtcNow - startTime,
                    SummaryMessage = $"Successfully imported {successCount} PSTN records.",
                    DetailedResults = JsonSerializer.Serialize(detailedResult)
                };

                _context.ImportAudits.Add(audit);
                await _context.SaveChangesAsync();

                // Now update all the PSTN records with the ImportAuditId
                var pstnsToUpdate = _context.PSTNs
                    .Where(p => p.ImportAuditId == null && p.CreatedDate >= startTime)
                    .ToList();

                foreach (var pstn in pstnsToUpdate)
                {
                    pstn.ImportAuditId = audit.Id;
                }

                await _context.SaveChangesAsync();

                StatusMessage = $"PSTN import completed: {successCount} records imported, {errorCount} errors.";
                StatusMessageClass = successCount > 0 ? "success" : "warning";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing PSTN CSV file");
                StatusMessage = $"PSTN Import failed: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        private async Task<IActionResult> ImportPrivateWireDataAsync(StreamReader reader, string[] headers, string headerLine, bool updateExisting, int billingMonth, int billingYear, string billingPeriodString, string dateFormat)
        {
            var startTime = DateTime.UtcNow;
            var importResults = new List<string>();
            var successCount = 0;
            var errorCount = 0;
            var updateCount = 0;
            var totalRecords = 0;
            var detailedResult = new ImportDetailedResult();

            try
            {
                // Validate Private Wire required columns - updated for normalized structure
                var requiredPWColumns = new[] { "inde_", "dialed", "date", "usd" };

                foreach (var required in requiredPWColumns)
                {
                    if (!headers.Any(h => h.Trim().Equals(required, StringComparison.OrdinalIgnoreCase)))
                    {
                        StatusMessage = $"Required Private Wire column '{required}' not found in CSV file.";
                        StatusMessageClass = "danger";
                        return RedirectToPage();
                    }
                }

                // Create column index mapping
                var columnIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < headers.Length; i++)
                {
                    columnIndices[headers[i].Trim()] = i;
                }

                var lineNumber = 1;
                var originalLines = new Dictionary<int, string>();

                while (!reader.EndOfStream)
                {
                    lineNumber++;
                    var line = await reader.ReadLineAsync();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Parse values first to check if row is truly empty
                    var values = ParseCsvLine(line);

                    // Skip rows where all values are empty or whitespace
                    if (values.All(v => string.IsNullOrWhiteSpace(v)))
                        continue;

                    totalRecords++;
                    originalLines[lineNumber] = line;

                    try
                    {
                        if (values.Length != headers.Length)
                        {
                            importResults.Add($"Line {lineNumber}: Invalid number of columns");
                            errorCount++;
                            detailedResult.Errors.Add(new ImportErrorDetail
                            {
                                LineNumber = lineNumber,
                                OriginalData = line,
                                ErrorMessage = $"Expected {headers.Length} columns, found {values.Length}",
                                FieldName = "Column Count"
                            });
                            continue;
                        }
                        // Parse date using flexible parser
                        var dateParse = ParseDateField(values[columnIndices["date"]], dateFormat, lineNumber, "date");
                        if (!dateParse.Success)
                        {
                            importResults.Add($"Line {lineNumber}: {dateParse.ErrorMessage}");
                            errorCount++;
                            detailedResult.Errors.Add(new ImportErrorDetail
                            {
                                LineNumber = lineNumber,
                                OriginalData = line,
                                ErrorMessage = dateParse.ErrorMessage,
                                FieldName = "date"
                            });
                            continue;
                        }
                        var date = dateParse.Date!.Value;

                        // Parse amount in USD
                        if (!decimal.TryParse(values[columnIndices["usd"]], out var usd))
                        {
                            importResults.Add($"Line {lineNumber}: Invalid amount format");
                            errorCount++;
                            detailedResult.Errors.Add(new ImportErrorDetail
                            {
                                LineNumber = lineNumber,
                                OriginalData = line,
                                ErrorMessage = $"Invalid amount format: '{values[columnIndices["usd"]]}'",
                                FieldName = "usd"
                            });
                            continue;
                        }

                        // Parse optional time field
                        TimeSpan? time = null;
                        if (columnIndices.ContainsKey("time") && !string.IsNullOrEmpty(values[columnIndices["time"]]))
                        {
                            TimeSpan.TryParse(values[columnIndices["time"]], out var parsedTime);
                            time = parsedTime;
                        }

                        // Parse optional duration fields
                        decimal? dur = null, durx = null;
                        if (columnIndices.ContainsKey("dur") && !string.IsNullOrEmpty(values[columnIndices["dur"]]))
                        {
                            decimal.TryParse(values[columnIndices["dur"]], out var parsedDur);
                            dur = parsedDur;
                        }
                        if (columnIndices.ContainsKey("durx") && !string.IsNullOrEmpty(values[columnIndices["durx"]]))
                        {
                            decimal.TryParse(values[columnIndices["durx"]], out var parsedDurx);
                            durx = parsedDurx;
                        }

                        // Try to match with EbillUser by IndexNumber
                        var dialedNumber = values[columnIndices["dialed"]];
                        var indexNumber = columnIndices.ContainsKey("inde_") ? values[columnIndices["inde_"]].Trim() : null;
                        var extension = columnIndices.ContainsKey("ext") ? values[columnIndices["ext"]].Trim() : null;

                        // First try to lookup by Extension in UserPhones table
                        UserPhone? userPhone = null;
                        EbillUser? ebillUser = null;

                        if (!string.IsNullOrEmpty(extension))
                        {
                            userPhone = await _context.UserPhones
                                .Include(up => up.EbillUser)
                                .FirstOrDefaultAsync(up => up.PhoneNumber == extension && up.IsActive);

                            if (userPhone != null)
                            {
                                // Get user info from UserPhone relationship
                                ebillUser = userPhone.EbillUser;
                                indexNumber = userPhone.IndexNumber; // Override with UserPhone's IndexNumber
                            }
                        }

                        // Fallback: If no UserPhone found but IndexNumber exists in CSV, try to match EbillUser directly
                        if (ebillUser == null && !string.IsNullOrEmpty(indexNumber))
                        {
                            ebillUser = await _context.EbillUsers
                                .FirstOrDefaultAsync(e => e.IndexNumber == indexNumber);
                        }

                        // Calculate KSH amount from USD using exchange rate
                        decimal? amountKSH = null;
                        if (usd > 0)
                        {
                            var exchangeRate = await _context.ExchangeRates
                                .FirstOrDefaultAsync(r => r.Month == billingMonth && r.Year == billingYear);

                            if (exchangeRate != null)
                            {
                                // Calculate: USD * Rate = KSH
                                // Example: 100 USD * 150 = 15000 KSH
                                amountKSH = Math.Round(usd * exchangeRate.Rate, 4);
                            }
                            else
                            {
                                _logger.LogWarning("No exchange rate found for {Month}/{Year}. AmountKSH will not be calculated for Private Wire.", billingMonth, billingYear);
                            }
                        }

                        var privateWire = new PrivateWire
                        {
                            Extension = extension,
                            DialedNumber = dialedNumber,
                            CallTime = time,
                            Destination = columnIndices.ContainsKey("dest") ? values[columnIndices["dest"]] : null,
                            DestinationLine = columnIndices.ContainsKey("dl") ? values[columnIndices["dl"]] : null,
                            DurationExtended = durx,
                            // Organization info obtained through EbillUserId relationship
                            CallDate = date,
                            Duration = dur,
                            AmountUSD = usd,
                            AmountKSH = amountKSH,
                            IndexNumber = indexNumber,
                            BillingPeriod = billingPeriodString,  // Use provided billing period
                            CallMonth = billingMonth,  // Use provided billing month
                            CallYear = billingYear,    // Use provided billing year
                            UserPhoneId = userPhone?.Id,  // Link to UserPhone if found by extension
                            EbillUserId = ebillUser?.Id,  // Link to EbillUser if found
                            CreatedDate = DateTime.UtcNow,
                            CreatedBy = User.Identity?.Name ?? "Unknown"
                        };

                        _context.PrivateWires.Add(privateWire);
                        successCount++;

                        detailedResult.Successes.Add(new ImportSuccessDetail
                        {
                            LineNumber = lineNumber,
                            RecordId = 0, // Will be updated after save
                            Summary = $"Private Wire record - {values[columnIndices["dialed"]]}"
                        });
                    }
                    catch (Exception lineEx)
                    {
                        importResults.Add($"Line {lineNumber}: {lineEx.Message}");
                        errorCount++;
                        detailedResult.Errors.Add(new ImportErrorDetail
                        {
                            LineNumber = lineNumber,
                            OriginalData = line,
                            ErrorMessage = lineEx.Message,
                            FieldName = "General"
                        });
                    }
                }

                // Create import audit first to get the ID
                var audit = new ImportAudit
                {
                    ImportType = "PrivateWire",
                    FileName = headerLine,
                    FileSize = 0, // We don't have the original file size here
                    TotalRecords = totalRecords,
                    SuccessCount = successCount,
                    UpdatedCount = updateCount,
                    SkippedCount = 0,
                    ErrorCount = errorCount,
                    ImportDate = DateTime.UtcNow,
                    ImportedBy = User.Identity?.Name ?? "Unknown",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    ProcessingTime = DateTime.UtcNow - startTime,
                    SummaryMessage = $"Successfully imported {successCount} Private Wire records.",
                    DetailedResults = JsonSerializer.Serialize(detailedResult)
                };

                _context.ImportAudits.Add(audit);
                await _context.SaveChangesAsync();

                // Now update all the PrivateWire records with the ImportAuditId
                var privateWiresToUpdate = _context.PrivateWires
                    .Where(p => p.ImportAuditId == null && p.CreatedDate >= startTime)
                    .ToList();

                foreach (var pw in privateWiresToUpdate)
                {
                    pw.ImportAuditId = audit.Id;
                }

                await _context.SaveChangesAsync();

                StatusMessage = $"Private Wire import completed: {successCount} records imported, {errorCount} errors.";
                StatusMessageClass = successCount > 0 ? "success" : "warning";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing Private Wire CSV file");
                StatusMessage = $"Private Wire Import failed: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        private async Task<IActionResult> ImportSafaricomDataAsync(StreamReader reader, string[] headers, string headerLine, bool updateExisting, int billingMonth, int billingYear, string billingPeriodString, string dateFormat)
        {
            var startTime = DateTime.UtcNow;
            var importResults = new List<string>();
            var successCount = 0;
            var errorCount = 0;
            var updateCount = 0;
            var totalRecords = 0;
            var detailedResult = new ImportDetailedResult();

            try
            {
                // Validate Safaricom required columns - updated for normalized structure
                var requiredSafColumns = new[] { "IndexNumber", "ext", "call_date", "dialed", "cost" };

                foreach (var required in requiredSafColumns)
                {
                    if (!headers.Any(h => h.Trim().Equals(required, StringComparison.OrdinalIgnoreCase)))
                    {
                        StatusMessage = $"Required Safaricom column '{required}' not found in CSV file.";
                        StatusMessageClass = "danger";
                        return RedirectToPage();
                    }
                }

                // Create column index mapping
                var columnIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < headers.Length; i++)
                {
                    columnIndices[headers[i].Trim()] = i;
                }

                var lineNumber = 1;
                var originalLines = new Dictionary<int, string>();

                while (!reader.EndOfStream)
                {
                    lineNumber++;
                    var line = await reader.ReadLineAsync();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Parse values first to check if row is truly empty
                    var values = ParseCsvLine(line);

                    // Skip rows where all values are empty or whitespace
                    if (values.All(v => string.IsNullOrWhiteSpace(v)))
                        continue;

                    totalRecords++;
                    originalLines[lineNumber] = line;

                    try
                    {
                        if (values.Length != headers.Length)
                        {
                            importResults.Add($"Line {lineNumber}: Invalid number of columns");
                            errorCount++;
                            detailedResult.Errors.Add(new ImportErrorDetail
                            {
                                LineNumber = lineNumber,
                                OriginalData = line,
                                ErrorMessage = $"Expected {headers.Length} columns, found {values.Length}",
                                FieldName = "Column Count"
                            });
                            continue;
                        }

                        // Parse call date using flexible parser
                        var dateParse = ParseDateField(values[columnIndices["call_date"]], dateFormat, lineNumber, "call_date");
                        if (!dateParse.Success)
                        {
                            importResults.Add($"Line {lineNumber}: {dateParse.ErrorMessage}");
                            errorCount++;
                            detailedResult.Errors.Add(new ImportErrorDetail
                            {
                                LineNumber = lineNumber,
                                OriginalData = line,
                                ErrorMessage = dateParse.ErrorMessage,
                                FieldName = "call_date",
                                FieldValue = values[columnIndices["call_date"]],
                                ErrorType = "DateFormatError",
                                SuggestedFix = $"Verify date format matches {dateFormat}"
                            });
                            continue;
                        }
                        var callDate = dateParse.Date!.Value;

                        // Parse cost
                        if (!decimal.TryParse(values[columnIndices["cost"]], out var cost))
                        {
                            importResults.Add($"Line {lineNumber}: Invalid cost format");
                            errorCount++;
                            detailedResult.Errors.Add(new ImportErrorDetail
                            {
                                LineNumber = lineNumber,
                                OriginalData = line,
                                ErrorMessage = $"Invalid cost format: '{values[columnIndices["cost"]]}'",
                                FieldName = "cost"
                            });
                            continue;
                        }

                        // Parse optional time field
                        TimeSpan? callTime = null;
                        if (columnIndices.ContainsKey("call_time") && !string.IsNullOrEmpty(values[columnIndices["call_time"]]))
                        {
                            TimeSpan.TryParse(values[columnIndices["call_time"]], out var parsedTime);
                            callTime = parsedTime;
                        }

                        // Parse optional duration fields
                        decimal? dur = null, durx = null;
                        if (columnIndices.ContainsKey("dur") && !string.IsNullOrEmpty(values[columnIndices["dur"]]))
                        {
                            decimal.TryParse(values[columnIndices["dur"]], out var parsedDur);
                            dur = parsedDur;
                        }
                        if (columnIndices.ContainsKey("durx") && !string.IsNullOrEmpty(values[columnIndices["durx"]]))
                        {
                            decimal.TryParse(values[columnIndices["durx"]], out var parsedDurx);
                            durx = parsedDurx;
                        }

                        // Parse optional month and year
                        int? callMonth = null, callYear = null;
                        if (columnIndices.ContainsKey("call_month") && !string.IsNullOrEmpty(values[columnIndices["call_month"]]))
                        {
                            int.TryParse(values[columnIndices["call_month"]], out var parsedMonth);
                            callMonth = parsedMonth;
                        }
                        if (columnIndices.ContainsKey("call_year") && !string.IsNullOrEmpty(values[columnIndices["call_year"]]))
                        {
                            int.TryParse(values[columnIndices["call_year"]], out var parsedYear);
                            callYear = parsedYear;
                        }

                        // Try to match with EbillUser by IndexNumber
                        var dialedNumber = values[columnIndices["dialed"]];
                        var indexNumber = columnIndices.ContainsKey("inde_") ? values[columnIndices["inde_"]].Trim() : null;
                        var extension = values[columnIndices["ext"]].Trim();

                        // First try to lookup by Extension in UserPhones table
                        UserPhone? userPhone = null;
                        EbillUser? ebillUser = null;

                        if (!string.IsNullOrEmpty(extension))
                        {
                            userPhone = await _context.UserPhones
                                .Include(up => up.EbillUser)
                                .FirstOrDefaultAsync(up => up.PhoneNumber == extension && up.IsActive);

                            if (userPhone != null)
                            {
                                // Get user info from UserPhone relationship
                                ebillUser = userPhone.EbillUser;
                                indexNumber = userPhone.IndexNumber; // Override with UserPhone's IndexNumber
                            }
                        }

                        // Fallback: If no UserPhone found but IndexNumber exists in CSV, try to match EbillUser directly
                        if (ebillUser == null && !string.IsNullOrEmpty(indexNumber))
                        {
                            ebillUser = await _context.EbillUsers
                                .FirstOrDefaultAsync(e => e.IndexNumber == indexNumber);
                        }

                        // Calculate USD amount
                        var amountUSD = await CalculateUSDAmount(cost, billingMonth, billingYear);

                        var safaricom = new Safaricom
                        {
                            Ext = extension,
                            CallDate = callDate,
                            CallTime = callTime,
                            Dialed = dialedNumber,
                            Dest = columnIndices.ContainsKey("dest") ? values[columnIndices["dest"]] : null,
                            Durx = durx,
                            Cost = cost,
                            AmountUSD = amountUSD,
                            Dur = dur,
                            CallType = columnIndices.ContainsKey("call_type") ? values[columnIndices["call_type"]] : null,
                            CallMonth = billingMonth,  // Use provided billing month
                            CallYear = billingYear,    // Use provided billing year
                            IndexNumber = indexNumber,
                            BillingPeriod = billingPeriodString,  // Use provided billing period
                            UserPhoneId = userPhone?.Id,  // Link to UserPhone if found by extension
                            EbillUserId = ebillUser?.Id,  // Link to EbillUser if found
                            CreatedDate = DateTime.UtcNow,
                            CreatedBy = User.Identity?.Name ?? "Unknown"
                        };

                        _context.Safaricoms.Add(safaricom);
                        successCount++;

                        detailedResult.Successes.Add(new ImportSuccessDetail
                        {
                            LineNumber = lineNumber,
                            RecordId = 0, // Will be updated after save
                            Summary = $"Safaricom record: Ext {values[columnIndices["ext"]]} - {values[columnIndices["dialed"]]}"
                        });
                    }
                    catch (Exception lineEx)
                    {
                        importResults.Add($"Line {lineNumber}: {lineEx.Message}");
                        errorCount++;
                        detailedResult.Errors.Add(new ImportErrorDetail
                        {
                            LineNumber = lineNumber,
                            OriginalData = line,
                            ErrorMessage = lineEx.Message,
                            FieldName = "General"
                        });
                    }
                }

                // Save all Safaricom records first
                await _context.SaveChangesAsync();

                // Create import audit
                var audit = new ImportAudit
                {
                    ImportType = "Safaricom",
                    FileName = headerLine,
                    FileSize = 0, // We don't have the original file size here
                    TotalRecords = totalRecords,
                    SuccessCount = successCount,
                    UpdatedCount = updateCount,
                    SkippedCount = 0,
                    ErrorCount = errorCount,
                    ImportDate = DateTime.UtcNow,
                    ImportedBy = User.Identity?.Name ?? "Unknown",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    ProcessingTime = DateTime.UtcNow - startTime,
                    SummaryMessage = $"Successfully imported {successCount} Safaricom records.",
                    DetailedResults = JsonSerializer.Serialize(detailedResult)
                };

                _context.ImportAudits.Add(audit);
                await _context.SaveChangesAsync();

                // Now update all the Safaricom records with the ImportAuditId
                var safaricomsToUpdate = _context.Safaricoms
                    .Where(s => s.ImportAuditId == null && s.CreatedDate >= startTime)
                    .ToList();

                foreach (var saf in safaricomsToUpdate)
                {
                    saf.ImportAuditId = audit.Id;
                }

                await _context.SaveChangesAsync();

                StatusMessage = $"Safaricom import completed: {successCount} records imported, {errorCount} errors.";
                StatusMessageClass = successCount > 0 ? "success" : "warning";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing Safaricom CSV file");
                StatusMessage = $"Safaricom Import failed: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        private async Task<IActionResult> ImportAirtelDataAsync(StreamReader reader, string[] headers, string headerLine, bool updateExisting, int billingMonth, int billingYear, string billingPeriodString, string dateFormat)
        {
            var startTime = DateTime.UtcNow;
            var importResults = new List<string>();
            var successCount = 0;
            var errorCount = 0;
            var updateCount = 0;
            var totalRecords = 0;
            var detailedResult = new ImportDetailedResult();

            try
            {
                // Validate Airtel required columns - updated for normalized structure
                var requiredColumns = new[] { "IndexNumber", "ext", "call_date", "dialed", "cost" };

                foreach (var required in requiredColumns)
                {
                    if (!headers.Any(h => h.Trim().Equals(required, StringComparison.OrdinalIgnoreCase)))
                    {
                        StatusMessage = $"Required Airtel column '{required}' not found in CSV file.";
                        StatusMessageClass = "danger";
                        return RedirectToPage();
                    }
                }

                // Create column index mapping
                var columnIndices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < headers.Length; i++)
                {
                    columnIndices[headers[i].Trim()] = i;
                }

                var lineNumber = 1;
                var originalLines = new Dictionary<int, string>();

                while (!reader.EndOfStream)
                {
                    lineNumber++;
                    var line = await reader.ReadLineAsync();

                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    // Parse values first to check if row is truly empty
                    var values = ParseCsvLine(line);

                    // Skip rows where all values are empty or whitespace
                    if (values.All(v => string.IsNullOrWhiteSpace(v)))
                        continue;

                    totalRecords++;
                    originalLines[lineNumber] = line;

                    try
                    {
                        if (values.Length != headers.Length)
                        {
                            importResults.Add($"Line {lineNumber}: Invalid number of columns");
                            errorCount++;
                            detailedResult.Errors.Add(new ImportErrorDetail
                            {
                                LineNumber = lineNumber,
                                OriginalData = line,
                                ErrorMessage = $"Expected {headers.Length} columns, found {values.Length}",
                                FieldName = "Column Count"
                            });
                            continue;
                        }

                        // Parse call date using flexible parser
                        var dateParse = ParseDateField(values[columnIndices["call_date"]], dateFormat, lineNumber, "call_date");
                        if (!dateParse.Success)
                        {
                            importResults.Add($"Line {lineNumber}: {dateParse.ErrorMessage}");
                            errorCount++;
                            detailedResult.Errors.Add(new ImportErrorDetail
                            {
                                LineNumber = lineNumber,
                                OriginalData = line,
                                ErrorMessage = dateParse.ErrorMessage,
                                FieldName = "call_date",
                                FieldValue = values[columnIndices["call_date"]],
                                ErrorType = "DateFormatError",
                                SuggestedFix = $"Verify date format matches {dateFormat}"
                            });
                            continue;
                        }
                        var callDate = dateParse.Date!.Value;

                        // Parse cost
                        if (!decimal.TryParse(values[columnIndices["cost"]], out var cost))
                        {
                            importResults.Add($"Line {lineNumber}: Invalid cost format");
                            errorCount++;
                            detailedResult.Errors.Add(new ImportErrorDetail
                            {
                                LineNumber = lineNumber,
                                OriginalData = line,
                                ErrorMessage = $"Invalid cost format: '{values[columnIndices["cost"]]}'",
                                FieldName = "cost"
                            });
                            continue;
                        }

                        // Parse optional time field
                        TimeSpan? callTime = null;
                        if (columnIndices.ContainsKey("call_time") && !string.IsNullOrEmpty(values[columnIndices["call_time"]]))
                        {
                            TimeSpan.TryParse(values[columnIndices["call_time"]], out var parsedTime);
                            callTime = parsedTime;
                        }

                        // Parse optional duration fields
                        decimal? dur = null, durx = null;
                        if (columnIndices.ContainsKey("dur") && !string.IsNullOrEmpty(values[columnIndices["dur"]]))
                        {
                            decimal.TryParse(values[columnIndices["dur"]], out var parsedDur);
                            dur = parsedDur;
                        }
                        if (columnIndices.ContainsKey("durx") && !string.IsNullOrEmpty(values[columnIndices["durx"]]))
                        {
                            decimal.TryParse(values[columnIndices["durx"]], out var parsedDurx);
                            durx = parsedDurx;
                        }

                        // Parse optional month and year
                        int? callMonth = null, callYear = null;
                        if (columnIndices.ContainsKey("call_month") && !string.IsNullOrEmpty(values[columnIndices["call_month"]]))
                        {
                            int.TryParse(values[columnIndices["call_month"]], out var parsedMonth);
                            callMonth = parsedMonth;
                        }
                        if (columnIndices.ContainsKey("call_year") && !string.IsNullOrEmpty(values[columnIndices["call_year"]]))
                        {
                            int.TryParse(values[columnIndices["call_year"]], out var parsedYear);
                            callYear = parsedYear;
                        }

                        // Try to match with EbillUser by IndexNumber
                        var dialedNumber = values[columnIndices["dialed"]];
                        var indexNumber = columnIndices.ContainsKey("inde_") ? values[columnIndices["inde_"]].Trim() : null;
                        var extension = values[columnIndices["ext"]].Trim();

                        // First try to lookup by Extension in UserPhones table
                        UserPhone? userPhone = null;
                        EbillUser? ebillUser = null;

                        if (!string.IsNullOrEmpty(extension))
                        {
                            userPhone = await _context.UserPhones
                                .Include(up => up.EbillUser)
                                .FirstOrDefaultAsync(up => up.PhoneNumber == extension && up.IsActive);

                            if (userPhone != null)
                            {
                                // Get user info from UserPhone relationship
                                ebillUser = userPhone.EbillUser;
                                indexNumber = userPhone.IndexNumber; // Override with UserPhone's IndexNumber
                            }
                        }

                        // Fallback: If no UserPhone found but IndexNumber exists in CSV, try to match EbillUser directly
                        if (ebillUser == null && !string.IsNullOrEmpty(indexNumber))
                        {
                            ebillUser = await _context.EbillUsers
                                .FirstOrDefaultAsync(e => e.IndexNumber == indexNumber);
                        }

                        // Calculate USD amount
                        var amountUSD = await CalculateUSDAmount(cost, billingMonth, billingYear);

                        var airtel = new Airtel
                        {
                            Ext = extension,
                            CallDate = callDate,
                            CallTime = callTime,
                            Dialed = dialedNumber,
                            Dest = columnIndices.ContainsKey("dest") ? values[columnIndices["dest"]] : null,
                            Durx = durx,
                            Cost = cost,
                            AmountUSD = amountUSD,
                            Dur = dur,
                            CallType = columnIndices.ContainsKey("call_type") ? values[columnIndices["call_type"]] : null,
                            CallMonth = billingMonth,  // Use provided billing month
                            CallYear = billingYear,    // Use provided billing year
                            IndexNumber = indexNumber,
                            BillingPeriod = billingPeriodString,  // Use provided billing period
                            UserPhoneId = userPhone?.Id,  // Link to UserPhone if found by extension
                            EbillUserId = ebillUser?.Id,  // Link to EbillUser if found
                            CreatedDate = DateTime.UtcNow,
                            CreatedBy = User.Identity?.Name ?? "Unknown"
                        };

                        _context.Airtels.Add(airtel);
                        successCount++;

                        detailedResult.Successes.Add(new ImportSuccessDetail
                        {
                            LineNumber = lineNumber,
                            RecordId = 0, // Will be updated after save
                            Summary = $"Airtel record: Ext {values[columnIndices["ext"]]} - {values[columnIndices["dialed"]]}"
                        });
                    }
                    catch (Exception lineEx)
                    {
                        importResults.Add($"Line {lineNumber}: {lineEx.Message}");
                        errorCount++;
                        detailedResult.Errors.Add(new ImportErrorDetail
                        {
                            LineNumber = lineNumber,
                            OriginalData = line,
                            ErrorMessage = lineEx.Message,
                            FieldName = "General"
                        });
                    }
                }

                // Save all Airtel records first
                await _context.SaveChangesAsync();

                // Create import audit
                var audit = new ImportAudit
                {
                    ImportType = "Airtel",
                    FileName = headerLine,
                    FileSize = 0, // We don't have the original file size here
                    TotalRecords = totalRecords,
                    SuccessCount = successCount,
                    UpdatedCount = updateCount,
                    SkippedCount = 0,
                    ErrorCount = errorCount,
                    ImportDate = DateTime.UtcNow,
                    ImportedBy = User.Identity?.Name ?? "Unknown",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    ProcessingTime = DateTime.UtcNow - startTime,
                    SummaryMessage = $"Successfully imported {successCount} Airtel records.",
                    DetailedResults = JsonSerializer.Serialize(detailedResult)
                };

                _context.ImportAudits.Add(audit);
                await _context.SaveChangesAsync();

                // Now update all the Airtel records with the ImportAuditId
                var airtelsToUpdate = _context.Airtels
                    .Where(a => a.ImportAuditId == null && a.CreatedDate >= startTime)
                    .ToList();

                foreach (var airtel in airtelsToUpdate)
                {
                    airtel.ImportAuditId = audit.Id;
                }

                await _context.SaveChangesAsync();

                StatusMessage = $"Airtel import completed: {successCount} records imported, {errorCount} errors.";
                StatusMessageClass = successCount > 0 ? "success" : "warning";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing Airtel CSV file");
                StatusMessage = $"Airtel Import failed: {ex.Message}";
                StatusMessageClass = "danger";
            }

            return RedirectToPage();
        }

        private (bool Success, DateTime? Date, string ErrorMessage) ParseDateField(string dateValue, string dateFormat, int lineNumber, string fieldName)
        {
            var parseResult = _dateParser.ParseDate(dateValue, dateFormat);

            if (!parseResult.Success || !parseResult.ParsedDate.HasValue)
            {
                return (false, null, $"Invalid date format: '{dateValue}' - {parseResult.ErrorMessage}. Expected format: {dateFormat}");
            }

            return (true, parseResult.ParsedDate.Value, string.Empty);
        }

        private string[] ParseCsvLine(string line)
        {
            var values = new List<string>();
            var currentValue = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentValue.Append('"');
                        i++; // Skip the next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            values.Add(currentValue.ToString());
            return values.ToArray();
        }

        public async Task<IActionResult> OnPostBulkDeleteAsync([FromBody] BulkDeleteRequest request)
        {
            if (request == null || request.Ids == null || !request.Ids.Any())
            {
                return new JsonResult(new { success = false, message = "No records selected" });
            }

            var deletedCount = 0;
            var failedRecords = new List<int>();
            var stagedRecords = new List<int>();

            try
            {
                switch (request.Type?.ToLower())
                {
                    case "pstn":
                        var pstnRecords = await _context.PSTNs
                            .Where(p => request.Ids.Contains(p.Id))
                            .ToListAsync();

                        foreach (var record in pstnRecords)
                        {
                            if (record.StagingBatchId.HasValue)
                            {
                                stagedRecords.Add(record.Id);
                            }
                            else
                            {
                                _context.PSTNs.Remove(record);
                                deletedCount++;
                            }
                        }
                        break;

                    case "privatewire":
                    case "private wire":
                        var privateWireRecords = await _context.PrivateWires
                            .Where(p => request.Ids.Contains(p.Id))
                            .ToListAsync();

                        foreach (var record in privateWireRecords)
                        {
                            if (record.StagingBatchId.HasValue)
                            {
                                stagedRecords.Add(record.Id);
                            }
                            else
                            {
                                _context.PrivateWires.Remove(record);
                                deletedCount++;
                            }
                        }
                        break;

                    case "safaricom":
                        var safaricomRecords = await _context.Safaricoms
                            .Where(s => request.Ids.Contains(s.Id))
                            .ToListAsync();

                        foreach (var record in safaricomRecords)
                        {
                            if (record.StagingBatchId.HasValue)
                            {
                                stagedRecords.Add(record.Id);
                            }
                            else
                            {
                                _context.Safaricoms.Remove(record);
                                deletedCount++;
                            }
                        }
                        break;

                    case "airtel":
                        var airtelRecords = await _context.Airtels
                            .Where(a => request.Ids.Contains(a.Id))
                            .ToListAsync();

                        foreach (var record in airtelRecords)
                        {
                            if (record.StagingBatchId.HasValue)
                            {
                                stagedRecords.Add(record.Id);
                            }
                            else
                            {
                                _context.Airtels.Remove(record);
                                deletedCount++;
                            }
                        }
                        break;

                    default:
                        return new JsonResult(new { success = false, message = "Invalid record type" });
                }

                await _context.SaveChangesAsync();

                string message;
                if (stagedRecords.Any())
                {
                    if (deletedCount > 0)
                    {
                        message = $"Deleted {deletedCount} record(s). {stagedRecords.Count} record(s) could not be deleted because they are staged for verification.";
                    }
                    else
                    {
                        message = $"No records deleted. All {stagedRecords.Count} selected record(s) are staged for verification and cannot be deleted.";
                    }
                }
                else
                {
                    message = $"Successfully deleted {deletedCount} record(s)";
                }

                return new JsonResult(new
                {
                    success = deletedCount > 0,
                    deletedCount = deletedCount,
                    failedRecords = failedRecords,
                    stagedRecords = stagedRecords,
                    message = message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk delete operation");
                return new JsonResult(new
                {
                    success = false,
                    message = "An error occurred during bulk deletion",
                    deletedCount = deletedCount,
                    failedRecords = failedRecords
                });
            }
        }

        public class BulkDeleteRequest
        {
            public string Type { get; set; } = string.Empty;
            public List<int> Ids { get; set; } = new();
        }

        public async Task<IActionResult> OnPostDeleteImportBatchAsync(int importId)
        {
            try
            {
                var importAudit = await _context.ImportAudits.FindAsync(importId);
                if (importAudit == null)
                {
                    return new JsonResult(new { success = false, message = "Import batch not found" });
                }

                // Check if any records have been staged for verification
                var hasStagedRecords = false;
                var stagedCount = 0;

                switch (importAudit.ImportType?.ToLower())
                {
                    case "pstn":
                        stagedCount = await _context.PSTNs
                            .Where(p => p.ImportAuditId == importId && p.StagingBatchId != null)
                            .CountAsync();
                        hasStagedRecords = stagedCount > 0;
                        break;

                    case "privatewire":
                        stagedCount = await _context.PrivateWires
                            .Where(p => p.ImportAuditId == importId && p.StagingBatchId != null)
                            .CountAsync();
                        hasStagedRecords = stagedCount > 0;
                        break;

                    case "safaricom":
                        stagedCount = await _context.Safaricoms
                            .Where(s => s.ImportAuditId == importId && s.StagingBatchId != null)
                            .CountAsync();
                        hasStagedRecords = stagedCount > 0;
                        break;

                    case "airtel":
                        stagedCount = await _context.Airtels
                            .Where(a => a.ImportAuditId == importId && a.StagingBatchId != null)
                            .CountAsync();
                        hasStagedRecords = stagedCount > 0;
                        break;
                }

                if (hasStagedRecords)
                {
                    return new JsonResult(new
                    {
                        success = false,
                        message = $"Cannot delete this import batch. {stagedCount} record(s) have been staged for verification and are part of the approval workflow. Please remove them from staging first."
                    });
                }

                var deletedCount = 0;

                // Delete all records associated with this import batch
                switch (importAudit.ImportType?.ToLower())
                {
                    case "pstn":
                        var pstnRecords = await _context.PSTNs
                            .Where(p => p.ImportAuditId == importId)
                            .ToListAsync();
                        _context.PSTNs.RemoveRange(pstnRecords);
                        deletedCount = pstnRecords.Count;
                        break;

                    case "privatewire":
                        var privateWireRecords = await _context.PrivateWires
                            .Where(p => p.ImportAuditId == importId)
                            .ToListAsync();
                        _context.PrivateWires.RemoveRange(privateWireRecords);
                        deletedCount = privateWireRecords.Count;
                        break;

                    case "safaricom":
                        var safaricomRecords = await _context.Safaricoms
                            .Where(s => s.ImportAuditId == importId)
                            .ToListAsync();
                        _context.Safaricoms.RemoveRange(safaricomRecords);
                        deletedCount = safaricomRecords.Count;
                        break;

                    case "airtel":
                        var airtelRecords = await _context.Airtels
                            .Where(a => a.ImportAuditId == importId)
                            .ToListAsync();
                        _context.Airtels.RemoveRange(airtelRecords);
                        deletedCount = airtelRecords.Count;
                        break;
                }

                // Delete the import audit record
                _context.ImportAudits.Remove(importAudit);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted import batch {ImportId} with {Count} records by user {UserName}",
                    importId, deletedCount, User.Identity?.Name);

                return new JsonResult(new
                {
                    success = true,
                    message = $"Successfully deleted import batch #{importId} with {deletedCount} records",
                    deletedCount = deletedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting import batch {ImportId}", importId);
                return new JsonResult(new
                {
                    success = false,
                    message = "An error occurred while deleting the import batch"
                });
            }
        }

        /// <summary>
        /// Download error report for a specific import
        /// </summary>
        public async Task<IActionResult> OnGetDownloadErrorReportAsync(int importId)
        {
            try
            {
                var importAudit = await _context.ImportAudits
                    .FirstOrDefaultAsync(i => i.Id == importId);

                if (importAudit == null)
                {
                    return NotFound();
                }

                // Parse detailed results
                ImportDetailedResult? detailedResult = null;
                if (!string.IsNullOrEmpty(importAudit.DetailedResults))
                {
                    detailedResult = JsonSerializer.Deserialize<ImportDetailedResult>(importAudit.DetailedResults);
                }

                if (detailedResult == null || detailedResult.Errors.Count == 0)
                {
                    return Content("No errors found for this import.", "text/plain");
                }

                // Generate CSV content
                var csv = new StringBuilder();
                csv.AppendLine("Row,Column,Value,Error Type,Error Message,Suggested Fix");

                foreach (var error in detailedResult.Errors)
                {
                    csv.AppendLine($"{error.LineNumber}," +
                                 $"\"{error.FieldName ?? "N/A"}\"," +
                                 $"\"{EscapeCsv(error.FieldValue ?? error.OriginalData ?? "N/A")}\"," +
                                 $"\"{error.ErrorType ?? "ValidationError"}\"," +
                                 $"\"{EscapeCsv(error.ErrorMessage)}\"," +
                                 $"\"{EscapeCsv(error.SuggestedFix ?? "")}\"");
                }

                // Return as downloadable file
                var fileName = $"import_errors_{importAudit.ImportType}_{importAudit.ImportDate:yyyyMMdd_HHmmss}.csv";
                var bytes = Encoding.UTF8.GetBytes(csv.ToString());

                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading error report for import {ImportId}", importId);
                return Content("An error occurred while generating the error report.", "text/plain");
            }
        }

        /// <summary>
        /// Download import summary report
        /// </summary>
        public async Task<IActionResult> OnGetDownloadImportSummaryAsync(int importId)
        {
            try
            {
                var importAudit = await _context.ImportAudits
                    .FirstOrDefaultAsync(i => i.Id == importId);

                if (importAudit == null)
                {
                    return NotFound();
                }

                // Parse detailed results
                ImportDetailedResult? detailedResult = null;
                if (!string.IsNullOrEmpty(importAudit.DetailedResults))
                {
                    detailedResult = JsonSerializer.Deserialize<ImportDetailedResult>(importAudit.DetailedResults);
                }

                // Generate summary report
                var report = new StringBuilder();
                report.AppendLine("=".PadRight(70, '='));
                report.AppendLine($"IMPORT SUMMARY REPORT - {importAudit.ImportType?.ToUpper()}");
                report.AppendLine("=".PadRight(70, '='));
                report.AppendLine();
                report.AppendLine($"File Name:        {importAudit.FileName}");
                report.AppendLine($"File Size:        {FormatFileSize(importAudit.FileSize)}");
                report.AppendLine($"Import Date:      {importAudit.ImportDate:yyyy-MM-dd HH:mm:ss} UTC");
                report.AppendLine($"Imported By:      {importAudit.ImportedBy}");
                report.AppendLine($"Processing Time:  {importAudit.ProcessingTime.TotalSeconds:F2} seconds");
                report.AppendLine();
                report.AppendLine("-".PadRight(70, '-'));
                report.AppendLine("RESULTS");
                report.AppendLine("-".PadRight(70, '-'));
                report.AppendLine($"Total Records:    {importAudit.TotalRecords}");
                report.AppendLine($"✓ Successful:     {importAudit.SuccessCount}");
                report.AppendLine($"↻ Updated:        {importAudit.UpdatedCount}");
                report.AppendLine($"⊘ Skipped:        {importAudit.SkippedCount}");
                report.AppendLine($"✗ Errors:         {importAudit.ErrorCount}");
                report.AppendLine();

                if (detailedResult != null && detailedResult.Errors.Count > 0)
                {
                    report.AppendLine("-".PadRight(70, '-'));
                    report.AppendLine($"ERROR DETAILS (First {Math.Min(20, detailedResult.Errors.Count)} of {detailedResult.Errors.Count})");
                    report.AppendLine("-".PadRight(70, '-'));

                    foreach (var error in detailedResult.Errors.Take(20))
                    {
                        report.AppendLine($"Row {error.LineNumber}: {error.ErrorMessage}");
                        if (!string.IsNullOrEmpty(error.FieldName))
                        {
                            report.AppendLine($"  Field: {error.FieldName}");
                        }
                        if (!string.IsNullOrEmpty(error.FieldValue))
                        {
                            report.AppendLine($"  Value: {error.FieldValue}");
                        }
                        report.AppendLine();
                    }

                    if (detailedResult.Errors.Count > 20)
                    {
                        report.AppendLine($"... and {detailedResult.Errors.Count - 20} more errors.");
                        report.AppendLine("Download the full error report CSV for complete details.");
                    }
                }

                report.AppendLine();
                report.AppendLine("=".PadRight(70, '='));
                report.AppendLine("END OF REPORT");
                report.AppendLine("=".PadRight(70, '='));

                // Return as downloadable file
                var fileName = $"import_summary_{importAudit.ImportType}_{importAudit.ImportDate:yyyyMMdd_HHmmss}.txt";
                var bytes = Encoding.UTF8.GetBytes(report.ToString());

                return File(bytes, "text/plain", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading import summary for import {ImportId}", importId);
                return Content("An error occurred while generating the import summary.", "text/plain");
            }
        }

        /// <summary>
        /// Escape CSV values
        /// </summary>
        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            // Escape quotes and wrap in quotes if contains comma, quote, or newline
            value = value.Replace("\"", "\"\"");
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            {
                return value;
            }
            return value;
        }

        /// <summary>
        /// Format file size in human-readable format
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Calculate USD amount from KES amount using exchange rate for the given month/year
        /// </summary>
        /// <param name="kesAmount">Amount in Kenyan Shillings</param>
        /// <param name="month">Billing month (1-12)</param>
        /// <param name="year">Billing year</param>
        /// <returns>Amount in USD or null if no exchange rate found</returns>
        private async Task<decimal?> CalculateUSDAmount(decimal? kesAmount, int month, int year)
        {
            if (!kesAmount.HasValue || kesAmount.Value == 0)
                return null;

            // Get exchange rate for the billing period
            var exchangeRate = await _context.ExchangeRates
                .FirstOrDefaultAsync(r => r.Month == month && r.Year == year);

            if (exchangeRate == null)
            {
                _logger.LogWarning("No exchange rate found for {Month}/{Year}. USD amount will not be calculated.", month, year);
                return null;
            }

            // Calculate: KES / Rate = USD
            // Example: 15000 KES / 150 = 100 USD
            return Math.Round(kesAmount.Value / exchangeRate.Rate, 4);
        }

        #region Unlinked Extension Manager

        /// <summary>
        /// Get aggregated summary of unlinked extensions across all telecom tables
        /// </summary>
        public async Task<IActionResult> OnGetUnlinkedExtensionsSummaryAsync()
        {
            try
            {
                // PSTN - uses "Extension" field
                var pstnUnlinked = await _context.PSTNs
                    .Where(p => p.EbillUserId == null && !string.IsNullOrEmpty(p.Extension))
                    .GroupBy(p => p.Extension)
                    .Select(g => new
                    {
                        Extension = g.Key,
                        Count = g.Count(),
                        Provider = "PSTN",
                        MinDate = g.Min(x => x.CallDate),
                        MaxDate = g.Max(x => x.CallDate)
                    }).ToListAsync();

                // PrivateWire - uses "Extension" field
                var privateWireUnlinked = await _context.PrivateWires
                    .Where(p => p.EbillUserId == null && !string.IsNullOrEmpty(p.Extension))
                    .GroupBy(p => p.Extension)
                    .Select(g => new
                    {
                        Extension = g.Key,
                        Count = g.Count(),
                        Provider = "Private Wire",
                        MinDate = g.Min(x => x.CallDate),
                        MaxDate = g.Max(x => x.CallDate)
                    }).ToListAsync();

                // Safaricom - uses "Ext" field
                var safaricomUnlinked = await _context.Safaricoms
                    .Where(s => s.EbillUserId == null && !string.IsNullOrEmpty(s.Ext))
                    .GroupBy(s => s.Ext)
                    .Select(g => new
                    {
                        Extension = g.Key,
                        Count = g.Count(),
                        Provider = "Safaricom",
                        MinDate = g.Min(x => x.CallDate),
                        MaxDate = g.Max(x => x.CallDate)
                    }).ToListAsync();

                // Airtel - uses "Ext" field
                var airtelUnlinked = await _context.Airtels
                    .Where(a => a.EbillUserId == null && !string.IsNullOrEmpty(a.Ext))
                    .GroupBy(a => a.Ext)
                    .Select(g => new
                    {
                        Extension = g.Key,
                        Count = g.Count(),
                        Provider = "Airtel",
                        MinDate = g.Min(x => x.CallDate),
                        MaxDate = g.Max(x => x.CallDate)
                    }).ToListAsync();

                // Combine all results into a single list for aggregation
                var allUnlinked = pstnUnlinked
                    .Select(x => new { x.Extension, x.Count, x.Provider, x.MinDate, x.MaxDate })
                    .Concat(privateWireUnlinked.Select(x => new { x.Extension, x.Count, x.Provider, x.MinDate, x.MaxDate }))
                    .Concat(safaricomUnlinked.Select(x => new { x.Extension, x.Count, x.Provider, x.MinDate, x.MaxDate }))
                    .Concat(airtelUnlinked.Select(x => new { x.Extension, x.Count, x.Provider, x.MinDate, x.MaxDate }))
                    .ToList();

                // Aggregate by extension across all providers
                var combined = allUnlinked
                    .GroupBy(x => x.Extension)
                    .Select(g => new
                    {
                        extension = g.Key,
                        totalRecords = g.Sum(x => x.Count),
                        providers = g.Select(x => x.Provider).Distinct().ToList(),
                        minDate = g.Min(x => x.MinDate),
                        maxDate = g.Max(x => x.MaxDate),
                        dateRange = $"{g.Min(x => x.MinDate):yyyy-MM-dd} to {g.Max(x => x.MaxDate):yyyy-MM-dd}"
                    })
                    .OrderByDescending(x => x.totalRecords)
                    .ToList();

                return new JsonResult(combined);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching unlinked extensions summary");
                return new JsonResult(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Search users for linking - supports name, email, index number search
        /// </summary>
        public async Task<IActionResult> OnGetSearchUsersForLinkingAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return new JsonResult(new List<object>());
            }

            var searchLower = term.ToLower();

            var users = await _context.EbillUsers
                .Include(u => u.OrganizationEntity)
                .Include(u => u.OfficeEntity)
                .Where(u =>
                    u.FirstName.ToLower().Contains(searchLower) ||
                    u.LastName.ToLower().Contains(searchLower) ||
                    u.IndexNumber.ToLower().Contains(searchLower) ||
                    (u.Email != null && u.Email.ToLower().Contains(searchLower)) ||
                    (u.OfficialMobileNumber != null && u.OfficialMobileNumber.Contains(term)))
                .Take(15)
                .Select(u => new
                {
                    id = u.Id,
                    fullName = u.FirstName + " " + u.LastName,
                    indexNumber = u.IndexNumber,
                    email = u.Email ?? "",
                    phone = u.OfficialMobileNumber ?? "",
                    organizationName = u.OrganizationEntity != null ? u.OrganizationEntity.Name : "N/A",
                    officeName = u.OfficeEntity != null ? u.OfficeEntity.Name : "N/A"
                })
                .ToListAsync();

            return new JsonResult(users);
        }

        /// <summary>
        /// Check if user has an existing primary phone
        /// </summary>
        public async Task<IActionResult> OnGetCheckExistingPrimaryPhoneAsync(string indexNumber)
        {
            try
            {
                var existingPrimary = await _context.UserPhones
                    .Where(up => up.IndexNumber == indexNumber && up.IsPrimary && up.IsActive)
                    .Select(up => new
                    {
                        hasPrimary = true,
                        phoneNumber = up.PhoneNumber,
                        phoneType = up.PhoneType
                    })
                    .FirstOrDefaultAsync();

                if (existingPrimary != null)
                {
                    return new JsonResult(existingPrimary);
                }

                return new JsonResult(new { hasPrimary = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking existing primary phone for {IndexNumber}", indexNumber);
                return new JsonResult(new { hasPrimary = false });
            }
        }

        /// <summary>
        /// Get Class of Service options for the extension registration dropdown
        /// </summary>
        public async Task<IActionResult> OnGetClassOfServiceOptionsAsync()
        {
            try
            {
                var options = await _context.ClassOfServices
                    .Where(c => c.ServiceStatus == ServiceStatus.Active)
                    .OrderBy(c => c.Class)
                    .ThenBy(c => c.Service)
                    .Select(c => new
                    {
                        id = c.Id,
                        name = $"{c.Class} - {c.Service}"
                    })
                    .ToListAsync();

                return new JsonResult(options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching class of service options");
                return new JsonResult(new List<object>());
            }
        }

        /// <summary>
        /// Bulk register extension as UserPhone and link all unlinked records with that extension to the user
        /// </summary>
        public async Task<IActionResult> OnPostBulkRegisterAndLinkAsync(
            string extension,
            int userId,
            string? indexNumber,
            string lineType,
            string phoneType,
            string ownershipType,
            int? classOfServiceId,
            string? location,
            string? notes)
        {
            // Use execution strategy to support SqlServerRetryingExecutionStrategy with transactions
            var strategy = _context.Database.CreateExecutionStrategy();

            try
            {
                var result = await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        var user = await _context.EbillUsers.FindAsync(userId);
                        if (user == null)
                        {
                            throw new InvalidOperationException("User not found");
                        }

                        // Use user's index number if not provided
                        var effectiveIndexNumber = string.IsNullOrWhiteSpace(indexNumber) ? user.IndexNumber : indexNumber;

                        // 1. Check if extension is already registered
                        var existingPhone = await _context.UserPhones
                            .FirstOrDefaultAsync(up => up.PhoneNumber == extension);

                        int userPhoneId;
                        bool isNewRegistration = false;

                        if (existingPhone == null)
                        {
                            // Parse enums
                            if (!Enum.TryParse<LineType>(lineType, out var parsedLineType))
                                parsedLineType = LineType.Secondary;

                            if (!Enum.TryParse<PhoneOwnershipType>(ownershipType, out var parsedOwnershipType))
                                parsedOwnershipType = PhoneOwnershipType.Personal;

                            // Determine if this is a primary line
                            bool isPrimary = parsedLineType == LineType.Primary;

                            // If setting as primary, unset other primary phones for this user
                            if (isPrimary)
                            {
                                var existingPrimaryPhones = await _context.UserPhones
                                    .Where(up => up.IndexNumber == user.IndexNumber && up.IsPrimary)
                                    .ToListAsync();

                                foreach (var phone in existingPrimaryPhones)
                                {
                                    phone.IsPrimary = false;
                                    phone.LineType = LineType.Secondary;
                                }
                            }

                            // Create new UserPhone record
                            var userPhone = new UserPhone
                            {
                                IndexNumber = user.IndexNumber,
                                PhoneNumber = extension,
                                PhoneType = phoneType ?? "Extension",
                                LineType = parsedLineType,
                                OwnershipType = parsedOwnershipType,
                                IsPrimary = isPrimary,
                                IsActive = true,
                                Status = PhoneStatus.Active,
                                ClassOfServiceId = classOfServiceId,
                                Location = location,
                                Notes = notes,
                                AssignedDate = DateTime.UtcNow,
                                CreatedDate = DateTime.UtcNow,
                                CreatedBy = User.Identity?.Name
                            };

                            _context.UserPhones.Add(userPhone);
                            await _context.SaveChangesAsync();
                            userPhoneId = userPhone.Id;
                            isNewRegistration = true;

                            _logger.LogInformation("Created new UserPhone registration for extension {Extension} linked to user {UserId}", extension, userId);
                        }
                        else
                        {
                            userPhoneId = existingPhone.Id;
                            _logger.LogInformation("Extension {Extension} already registered as UserPhoneId {UserPhoneId}", extension, userPhoneId);
                        }

                        // 2. Link all unlinked records across all tables
                        int linkedCount = 0;
                        var modifiedBy = User.Identity?.Name;
                        var modifiedDate = DateTime.UtcNow;

                        // PSTN
                        var pstnRecords = await _context.PSTNs
                            .Where(p => p.Extension == extension && p.EbillUserId == null)
                            .ToListAsync();
                        foreach (var record in pstnRecords)
                        {
                            record.EbillUserId = userId;
                            record.UserPhoneId = userPhoneId;
                            record.IndexNumber = user.IndexNumber;
                            record.ModifiedDate = modifiedDate;
                            record.ModifiedBy = modifiedBy;
                        }
                        linkedCount += pstnRecords.Count;

                        // PrivateWire
                        var privateWireRecords = await _context.PrivateWires
                            .Where(p => p.Extension == extension && p.EbillUserId == null)
                            .ToListAsync();
                        foreach (var record in privateWireRecords)
                        {
                            record.EbillUserId = userId;
                            record.UserPhoneId = userPhoneId;
                            record.IndexNumber = user.IndexNumber;
                            record.ModifiedDate = modifiedDate;
                            record.ModifiedBy = modifiedBy;
                        }
                        linkedCount += privateWireRecords.Count;

                        // Safaricom (uses Ext field)
                        var safaricomRecords = await _context.Safaricoms
                            .Where(s => s.Ext == extension && s.EbillUserId == null)
                            .ToListAsync();
                        foreach (var record in safaricomRecords)
                        {
                            record.EbillUserId = userId;
                            record.UserPhoneId = userPhoneId;
                            record.IndexNumber = user.IndexNumber;
                            record.ModifiedDate = modifiedDate;
                            record.ModifiedBy = modifiedBy;
                        }
                        linkedCount += safaricomRecords.Count;

                        // Airtel (uses Ext field)
                        var airtelRecords = await _context.Airtels
                            .Where(a => a.Ext == extension && a.EbillUserId == null)
                            .ToListAsync();
                        foreach (var record in airtelRecords)
                        {
                            record.EbillUserId = userId;
                            record.UserPhoneId = userPhoneId;
                            record.IndexNumber = user.IndexNumber;
                            record.ModifiedDate = modifiedDate;
                            record.ModifiedBy = modifiedBy;
                        }
                        linkedCount += airtelRecords.Count;

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        var message = isNewRegistration
                            ? $"Successfully registered extension {extension} and linked {linkedCount} record(s) to {user.FirstName} {user.LastName}"
                            : $"Extension was already registered. Linked {linkedCount} record(s) to {user.FirstName} {user.LastName}";

                        _logger.LogInformation("BulkRegisterAndLink completed: Extension={Extension}, UserId={UserId}, LinkedCount={LinkedCount}, NewRegistration={NewRegistration}",
                            extension, userId, linkedCount, isNewRegistration);

                        return new
                        {
                            success = true,
                            message = message,
                            linkedCount = linkedCount,
                            isNewRegistration = isNewRegistration,
                            userName = $"{user.FirstName} {user.LastName}",
                            userIndex = user.IndexNumber
                        };
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });

                return new JsonResult(result);
            }
            catch (InvalidOperationException ex) when (ex.Message == "User not found")
            {
                return new JsonResult(new { success = false, message = "User not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BulkRegisterAndLink for extension {Extension}, userId {UserId}", extension, userId);
                return new JsonResult(new { success = false, message = $"An error occurred: {ex.Message}" });
            }
        }

        #endregion
    }
}