using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Services;

namespace TAB.Web.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class EOSRecoveryModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EOSRecoveryModel> _logger;
        private readonly ICallLogRecoveryService _recoveryService;
        private const int PageSize = 20;

        public EOSRecoveryModel(
            ApplicationDbContext context,
            ILogger<EOSRecoveryModel> logger,
            ICallLogRecoveryService recoveryService)
        {
            _context = context;
            _logger = logger;
            _recoveryService = recoveryService;
        }

        [TempData]
        public string? SuccessMessage { get; set; }

        [TempData]
        public string? ErrorMessage { get; set; }

        // EOS Staff with pending recovery
        public List<EOSStaffRecovery> EOSStaffList { get; set; } = new();

        // Recovery Statistics
        public EOSRecoveryStatistics Statistics { get; set; } = new();

        // Pagination and Filtering
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
        public string SearchQuery { get; set; } = string.Empty;
        public string OrganizationFilter { get; set; } = string.Empty;
        public string SortBy { get; set; } = "amount";
        public List<string> Organizations { get; set; } = new();

        // Month and Year Filters
        public int? FilterMonth { get; set; }
        public int? FilterYear { get; set; }

        [BindProperty]
        public List<string> SelectedStaffIndexNumbers { get; set; } = new();

        public async Task OnGetAsync(int page = 1, string searchQuery = "", string organizationFilter = "", string sortBy = "amount", int? filterMonth = null, int? filterYear = null)
        {
            CurrentPage = page;
            SearchQuery = searchQuery;
            OrganizationFilter = organizationFilter;
            SortBy = sortBy;
            FilterMonth = filterMonth;
            FilterYear = filterYear;

            // Run queries sequentially - DbContext is not thread-safe
            await LoadEOSStaffDataAsync();
            await LoadStatisticsAsync();
        }

        /// <summary>
        /// Trigger EOS (End of Service) Recovery using the same business logic as regular recovery:
        /// - Personal calls: Full recovery of call cost
        /// - Official calls: NO recovery (certified as official business)
        /// This ensures consistency across the application's recovery process.
        /// </summary>
        public async Task<IActionResult> OnPostTriggerRecoveryAsync()
        {
            var failedStaff = new List<(string IndexNumber, string StaffName, string Error)>();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (SelectedStaffIndexNumbers == null || !SelectedStaffIndexNumbers.Any())
                {
                    ErrorMessage = "Please select at least one staff member to process recovery.";
                    await LoadEOSStaffDataAsync();
                    await LoadStatisticsAsync();
                    return Page();
                }

                _logger.LogInformation("EOS Recovery triggered for {Count} staff members by {User}",
                    SelectedStaffIndexNumbers.Count, User.Identity?.Name);

                var executionId = Guid.NewGuid();
                var executionTime = DateTime.UtcNow;

                int totalProcessed = 0;
                int totalSuccess = 0;
                int totalFailed = 0;
                decimal totalRecovered = 0;

                // Get all approved PERSONAL records in ONE query
                var allApprovedRecords = await _context.CallRecords
                    .Where(r => SelectedStaffIndexNumbers.Contains(r.ResponsibleIndexNumber) &&
                               r.SupervisorApprovalStatus == "Approved" &&
                               r.VerificationType == "Personal" &&
                               (r.RecoveryStatus == null || r.RecoveryStatus == "Pending" || r.RecoveryStatus == "NotProcessed"))
                    .ToListAsync();

                if (!allApprovedRecords.Any())
                {
                    await transaction.RollbackAsync();
                    ErrorMessage = "No Personal calls found for recovery. The selected staff members only have Official calls which are already certified as official business.";
                    await LoadEOSStaffDataAsync();
                    await LoadStatisticsAsync();
                    return Page();
                }

                // Get staff names in bulk for error reporting
                var staffNames = await _context.EbillUsers
                    .AsNoTracking()
                    .Where(u => SelectedStaffIndexNumbers.Contains(u.IndexNumber))
                    .Select(u => new { u.IndexNumber, u.FullName })
                    .ToDictionaryAsync(u => u.IndexNumber, u => u.FullName);

                // Group records by staff member
                var recordsByStaff = allApprovedRecords.GroupBy(r => r.ResponsibleIndexNumber);
                var recoveryLogsToAdd = new List<RecoveryLog>();

                foreach (var staffGroup in recordsByStaff)
                {
                    var indexNumber = staffGroup.Key;
                    var approvedRecords = staffGroup.ToList();
                    var staffName = staffNames.GetValueOrDefault(indexNumber ?? "", indexNumber ?? "Unknown");

                    try
                    {
                        if (!approvedRecords.Any())
                        {
                            _logger.LogWarning("No approved records found for EOS staff {IndexNumber}", indexNumber);
                            continue;
                        }

                        foreach (var record in approvedRecords)
                        {
                            totalProcessed++;

                            try
                            {
                                if (record.VerificationType != "Personal")
                                {
                                    _logger.LogWarning("Skipping non-Personal record {RecordId} - VerificationType: {Type}",
                                        record.Id, record.VerificationType);
                                    continue;
                                }

                                decimal recoveryAmount = record.CallCostUSD;

                                record.AssignmentStatus = "Personal";
                                record.FinalAssignmentType = "Personal";

                                var recoveryLog = new RecoveryLog
                                {
                                    CallRecordId = record.Id,
                                    RecoveryType = "EOS",
                                    RecoveryAction = "Personal",
                                    RecoveryDate = DateTime.UtcNow,
                                    RecoveryReason = $"EOS Recovery - Personal call recovered from staff {indexNumber} - Call on {record.CallDate:yyyy-MM-dd}",
                                    AmountRecovered = recoveryAmount,
                                    RecoveredFrom = record.ResponsibleIndexNumber,
                                    ProcessedBy = User.Identity?.Name ?? "System",
                                    IsAutomated = false,
                                    BatchId = record.SourceBatchId ?? Guid.Empty,
                                    Metadata = System.Text.Json.JsonSerializer.Serialize(new
                                    {
                                        CallMonth = record.CallMonth,
                                        CallYear = record.CallYear,
                                        CallCostUSD = record.CallCostUSD,
                                        CallCostKSH = record.CallCostKSHS,
                                        VerificationType = record.VerificationType,
                                        RecoveryMethod = "EOS Manual Trigger",
                                        ExecutionId = executionId.ToString(),
                                        ExecutionTime = executionTime
                                    })
                                };

                                recoveryLogsToAdd.Add(recoveryLog);

                                record.RecoveryStatus = "Completed";
                                record.RecoveryAmount = recoveryAmount;
                                record.RecoveryDate = DateTime.UtcNow;
                                record.RecoveryProcessedBy = User.Identity?.Name ?? "System";

                                totalRecovered += recoveryAmount;
                                totalSuccess++;

                                _logger.LogInformation("EOS Recovery processed for Personal record {RecordId}, Amount: ${Amount:F2}",
                                    record.Id, recoveryAmount);
                            }
                            catch (Exception recordEx)
                            {
                                totalFailed++;
                                _logger.LogError(recordEx, "Failed to process recovery for record {RecordId}", record.Id);
                                throw;
                            }
                        }
                    }
                    catch (Exception staffEx)
                    {
                        _logger.LogError(staffEx, "Failed to process recovery for staff {IndexNumber}", indexNumber);
                        failedStaff.Add((indexNumber ?? "Unknown", staffName, staffEx.Message));
                        totalFailed++;
                        await transaction.RollbackAsync();
                        throw;
                    }
                }

                // Bulk add recovery logs
                _context.RecoveryLogs.AddRange(recoveryLogsToAdd);
                await _context.SaveChangesAsync();

                // Update batch totals
                var processedBatchIds = allApprovedRecords
                    .Where(r => r.SourceBatchId.HasValue)
                    .Select(r => r.SourceBatchId!.Value)
                    .Distinct()
                    .ToList();

                var batchesToUpdate = await _context.StagingBatches
                    .Where(b => processedBatchIds.Contains(b.Id))
                    .ToListAsync();

                var executionIdString = executionId.ToString();

                // Calculate batch totals from the logs we just added
                var batchTotals = recoveryLogsToAdd
                    .Where(r => r.RecoveryAction == "Personal")
                    .GroupBy(r => r.BatchId)
                    .ToDictionary(g => g.Key, g => g.Sum(r => r.AmountRecovered));

                foreach (var batch in batchesToUpdate)
                {
                    if (batchTotals.TryGetValue(batch.Id, out var personalAmount))
                    {
                        batch.TotalPersonalAmount = (batch.TotalPersonalAmount ?? 0) + personalAmount;
                        batch.TotalRecoveredAmount = (batch.TotalRecoveredAmount ?? 0) + personalAmount;
                    }

                    var allRecordsRecovered = !await _context.CallRecords
                        .AsNoTracking()
                        .AnyAsync(r => r.SourceBatchId == batch.Id &&
                                      r.SupervisorApprovalStatus == "Approved" &&
                                      (r.RecoveryStatus == null || r.RecoveryStatus == "Pending" || r.RecoveryStatus == "NotProcessed"));

                    if (allRecordsRecovered)
                    {
                        batch.RecoveryStatus = "Completed";
                        batch.RecoveryProcessingDate = DateTime.UtcNow;
                        _logger.LogInformation("Batch {BatchId} marked as recovery completed", batch.Id);
                    }
                    else
                    {
                        batch.RecoveryStatus = "InProgress";
                        _logger.LogInformation("Batch {BatchId} marked as recovery in progress", batch.Id);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var personalRecordsProcessed = recoveryLogsToAdd.Count(r => r.RecoveryAction == "Personal");

                var successDetails = $"EOS Recovery completed! " +
                    $"Processed: {personalRecordsProcessed} Personal call(s) recovered. " +
                    $"Success: {totalSuccess}, Failed: {totalFailed}. " +
                    $"Total Recovered: ${totalRecovered:F2}";

                if (failedStaff.Any())
                {
                    var failedDetails = string.Join(", ", failedStaff.Select(f => $"{f.StaffName} ({f.IndexNumber})"));
                    successDetails += $"\n\nWarning: Some staff could not be processed: {failedDetails}";
                }

                SuccessMessage = successDetails;
                _logger.LogInformation("EOS Recovery completed. ExecutionId: {ExecutionId}, Personal: {Personal}, Total Recovered: ${TotalRecovered:F2}",
                    executionId, personalRecordsProcessed, totalRecovered);

                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering EOS recovery - transaction rolled back");

                var errorDetails = $"Error triggering recovery: {ex.Message}";

                if (failedStaff.Any())
                {
                    var failedDetails = string.Join(", ", failedStaff.Select(f => $"{f.StaffName} ({f.IndexNumber}): {f.Error}"));
                    errorDetails += $"\n\nFailed staff: {failedDetails}";
                }

                ErrorMessage = errorDetails + "\n\nAll changes have been rolled back.";
                await LoadEOSStaffDataAsync();
                await LoadStatisticsAsync();
                return Page();
            }
        }

        private async Task LoadEOSStaffDataAsync()
        {
            _logger.LogInformation("Starting EOS Recovery data load...");

            // Get published INTERIM batch IDs in one query
            var publishedInterimBatchIds = await _context.StagingBatches
                .AsNoTracking()
                .Where(b => b.BatchCategory == "INTERIM" && b.BatchStatus == BatchStatus.Published)
                .Select(b => b.Id)
                .ToListAsync();

            _logger.LogInformation($"Found {publishedInterimBatchIds.Count} Published INTERIM batches");

            if (!publishedInterimBatchIds.Any())
            {
                EOSStaffList = new List<EOSStaffRecovery>();
                Organizations = new List<string>();
                TotalRecords = 0;
                TotalPages = 0;
                return;
            }

            // Build base query for call records
            var baseQuery = _context.CallRecords
                .AsNoTracking()
                .Where(r => r.SourceBatchId.HasValue &&
                           publishedInterimBatchIds.Contains(r.SourceBatchId.Value) &&
                           r.IsVerified == true &&
                           (r.SupervisorApprovalStatus == "Approved" || r.SupervisorApprovalStatus == "Pending") &&
                           (r.RecoveryStatus == null || r.RecoveryStatus == "Pending" || r.RecoveryStatus == "NotProcessed"));

            // Apply month/year filters
            if (FilterMonth.HasValue)
            {
                baseQuery = baseQuery.Where(r => r.CallMonth == FilterMonth.Value);
            }
            if (FilterYear.HasValue)
            {
                baseQuery = baseQuery.Where(r => r.CallYear == FilterYear.Value);
            }

            // Get aggregated data per staff member in single query
            var staffRecoveryData = await baseQuery
                .GroupBy(r => r.ResponsibleIndexNumber)
                .Select(g => new
                {
                    IndexNumber = g.Key,
                    TotalRecords = g.Count(),
                    TotalPersonal = g.Where(r => r.VerificationType == "Personal").Sum(r => (decimal?)r.CallCostUSD) ?? 0,
                    TotalOfficial = g.Where(r => r.VerificationType == "Official").Sum(r => (decimal?)r.CallCostUSD) ?? 0,
                    ApprovedCount = g.Count(r => r.SupervisorApprovalStatus == "Approved"),
                    LatestBatchId = g.Select(r => r.SourceBatchId).FirstOrDefault(),
                    LatestCallDate = g.Max(r => r.CallDate),
                    RecentVerificationStatus = g.OrderByDescending(r => r.CallDate).Select(r => r.IsVerified).FirstOrDefault(),
                    RecentApprovalStatus = g.OrderByDescending(r => r.CallDate).Select(r => r.SupervisorApprovalStatus).FirstOrDefault(),
                    RecentVerificationDeadline = g.OrderByDescending(r => r.CallDate).Select(r => r.VerificationPeriod).FirstOrDefault(),
                    RecentApprovalDeadline = g.OrderByDescending(r => r.CallDate).Select(r => r.ApprovalPeriod).FirstOrDefault()
                })
                .Where(g => g.IndexNumber != null)
                .ToListAsync();

            _logger.LogInformation($"Found {staffRecoveryData.Count} staff with INTERIM records");

            if (!staffRecoveryData.Any())
            {
                EOSStaffList = new List<EOSStaffRecovery>();
                Organizations = new List<string>();
                TotalRecords = 0;
                TotalPages = 0;
                return;
            }

            // Get staff details in bulk
            var indexNumbers = staffRecoveryData.Select(s => s.IndexNumber!).ToList();
            var staffDetails = await _context.EbillUsers
                .AsNoTracking()
                .Include(u => u.OrganizationEntity)
                .Where(u => indexNumbers.Contains(u.IndexNumber))
                .Select(u => new
                {
                    u.IndexNumber,
                    u.FullName,
                    u.Email,
                    Organization = u.OrganizationEntity != null ? u.OrganizationEntity.Name : "N/A"
                })
                .ToDictionaryAsync(u => u.IndexNumber, u => u);

            // Get batch details in bulk
            var batchIds = staffRecoveryData
                .Where(s => s.LatestBatchId.HasValue)
                .Select(s => s.LatestBatchId!.Value)
                .Distinct()
                .ToList();

            var batchDetails = await _context.StagingBatches
                .AsNoTracking()
                .Where(b => batchIds.Contains(b.Id))
                .Select(b => new { b.Id, b.BatchName, b.CreatedDate })
                .ToDictionaryAsync(b => b.Id, b => b);

            // Build the staff list
            var allStaffList = new List<EOSStaffRecovery>();

            foreach (var data in staffRecoveryData)
            {
                if (data.IndexNumber == null) continue;

                var staff = staffDetails.GetValueOrDefault(data.IndexNumber);
                if (staff == null) continue;

                var batchInfo = data.LatestBatchId.HasValue
                    ? batchDetails.GetValueOrDefault(data.LatestBatchId.Value)
                    : null;

                allStaffList.Add(new EOSStaffRecovery
                {
                    IndexNumber = data.IndexNumber,
                    StaffName = staff.FullName,
                    Email = staff.Email,
                    Organization = staff.Organization,
                    BatchName = batchInfo?.BatchName ?? "N/A",
                    BatchDate = batchInfo?.CreatedDate ?? DateTime.MinValue,
                    TotalRecords = data.TotalRecords,
                    TotalPersonalAmount = data.TotalPersonal,
                    TotalOfficialAmount = data.TotalOfficial,
                    TotalRecoveryAmount = data.TotalPersonal, // Only Personal calls need recovery
                    ApprovedRecordsCount = data.ApprovedCount,
                    PendingRecovery = true,
                    VerificationStatus = data.RecentVerificationStatus == true ? "Verified" : "Pending",
                    VerificationDeadline = data.RecentVerificationDeadline,
                    SupervisorApprovalStatus = data.RecentApprovalStatus ?? "Pending",
                    SupervisorApprovalDeadline = data.RecentApprovalDeadline
                });

                _logger.LogInformation($"Added {staff.FullName} to EOS Recovery list with {data.TotalRecords} records totaling ${data.TotalPersonal:F2}");
            }

            _logger.LogInformation($"Total staff in EOS Recovery list before filtering: {allStaffList.Count}");

            // Get unique organizations for filter dropdown
            Organizations = allStaffList
                .Select(s => s.Organization)
                .Where(o => !string.IsNullOrEmpty(o) && o != "N/A")
                .Select(o => o!)
                .Distinct()
                .OrderBy(o => o)
                .ToList();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                allStaffList = allStaffList
                    .Where(s => s.StaffName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                               s.IndexNumber.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Apply organization filter
            if (!string.IsNullOrWhiteSpace(OrganizationFilter))
            {
                allStaffList = allStaffList.Where(s => s.Organization == OrganizationFilter).ToList();
            }

            // Apply sorting
            allStaffList = SortBy switch
            {
                "name" => allStaffList.OrderBy(s => s.StaffName).ToList(),
                "date" => allStaffList.OrderByDescending(s => s.BatchDate).ToList(),
                _ => allStaffList.OrderByDescending(s => s.TotalRecoveryAmount).ToList()
            };

            // Calculate pagination
            TotalRecords = allStaffList.Count;
            TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);

            if (CurrentPage < 1) CurrentPage = 1;
            if (CurrentPage > TotalPages && TotalPages > 0) CurrentPage = TotalPages;

            // Apply pagination
            EOSStaffList = allStaffList
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }

        private async Task LoadStatisticsAsync()
        {
            // Get EOS recovery stats in single query
            var eosStats = await _context.RecoveryLogs
                .AsNoTracking()
                .Where(r => r.RecoveryType == "EOS")
                .GroupBy(r => 1)
                .Select(g => new
                {
                    TotalAmount = g.Sum(r => r.AmountRecovered),
                    TotalRecords = g.Count(),
                    UniqueStaff = g.Select(r => r.RecoveredFrom).Distinct().Count(),
                    LastDate = g.Max(r => (DateTime?)r.RecoveryDate)
                })
                .FirstOrDefaultAsync();

            Statistics = new EOSRecoveryStatistics
            {
                TotalEOSStaff = EOSStaffList.Count,
                TotalPendingRecords = EOSStaffList.Sum(s => s.TotalRecords),
                TotalPendingAmount = EOSStaffList.Sum(s => s.TotalRecoveryAmount),
                TotalProcessedStaff = eosStats?.UniqueStaff ?? 0,
                TotalRecoveredAmount = eosStats?.TotalAmount ?? 0,
                TotalRecoveredRecords = eosStats?.TotalRecords ?? 0,
                LastProcessedDate = eosStats?.LastDate
            };
        }

        /// <summary>
        /// Get EOS Profile - Returns user profile and assigned phone numbers for EOS processing
        /// </summary>
        public async Task<JsonResult> OnGetEOSProfileAsync(string indexNumber)
        {
            try
            {
                var user = await _context.EbillUsers
                    .AsNoTracking()
                    .Include(u => u.OrganizationEntity)
                    .FirstOrDefaultAsync(u => u.IndexNumber == indexNumber);

                if (user == null)
                {
                    return new JsonResult(new { success = false, message = "User not found" });
                }

                var phones = await _context.UserPhones
                    .AsNoTracking()
                    .Include(p => p.ClassOfService)
                    .Where(p => p.IndexNumber == indexNumber && p.IsActive)
                    .OrderByDescending(p => p.IsPrimary)
                    .ThenBy(p => p.PhoneType)
                    .Select(p => new
                    {
                        id = p.Id,
                        phoneNumber = p.PhoneNumber,
                        phoneType = p.PhoneType,
                        status = p.Status.ToString(),
                        isPrimary = p.IsPrimary,
                        location = p.Location,
                        lineType = p.LineType.ToString(),
                        classOfService = p.ClassOfService != null ? $"{p.ClassOfService.Class} - {p.ClassOfService.Service}" : null
                    })
                    .ToListAsync();

                return new JsonResult(new
                {
                    success = true,
                    indexNumber = user.IndexNumber,
                    fullName = $"{user.FirstName} {user.LastName}",
                    email = user.Email,
                    organization = user.OrganizationEntity?.Name,
                    isActive = user.IsActive,
                    phones = phones
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading EOS profile for {IndexNumber}", indexNumber);
                return new JsonResult(new { success = false, message = "Error loading profile" });
            }
        }

        /// <summary>
        /// Search Staff - Search for staff members by name, email, or index number
        /// </summary>
        public async Task<JsonResult> OnGetSearchStaffAsync(string searchQuery)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchQuery) || searchQuery.Length < 3)
                {
                    return new JsonResult(new { success = false, message = "Search query must be at least 3 characters" });
                }

                var query = searchQuery.ToLower().Trim();

                var results = await _context.EbillUsers
                    .AsNoTracking()
                    .Include(u => u.OrganizationEntity)
                    .Where(u =>
                        u.FirstName.ToLower().Contains(query) ||
                        u.LastName.ToLower().Contains(query) ||
                        u.Email.ToLower().Contains(query) ||
                        u.IndexNumber.ToLower().Contains(query))
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .Take(10)
                    .Select(u => new
                    {
                        indexNumber = u.IndexNumber,
                        fullName = $"{u.FirstName} {u.LastName}",
                        email = u.Email,
                        organization = u.OrganizationEntity != null ? u.OrganizationEntity.Name : null
                    })
                    .ToListAsync();

                return new JsonResult(new { success = true, results = results });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching staff with query {Query}", searchQuery);
                return new JsonResult(new { success = false, message = "Error searching staff" });
            }
        }

        /// <summary>
        /// Reassign Phone - Transfer phone from EOS staff to another staff member
        /// </summary>
        public async Task<JsonResult> OnPostReassignPhoneAsync(int phoneId, string newIndexNumber)
        {
            try
            {
                var phone = await _context.UserPhones
                    .Include(p => p.EbillUser)
                    .FirstOrDefaultAsync(p => p.Id == phoneId);

                if (phone == null)
                {
                    return new JsonResult(new { success = false, message = "Phone record not found" });
                }

                var newUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.IndexNumber == newIndexNumber);

                if (newUser == null)
                {
                    return new JsonResult(new { success = false, message = "New staff member not found" });
                }

                var oldIndexNumber = phone.IndexNumber;
                var oldUserName = phone.EbillUser != null ? $"{phone.EbillUser.FirstName} {phone.EbillUser.LastName}" : oldIndexNumber;

                if (phone.IsPrimary)
                {
                    var existingPrimary = await _context.UserPhones
                        .FirstOrDefaultAsync(p => p.IndexNumber == newIndexNumber && p.IsPrimary && p.IsActive);

                    if (existingPrimary != null)
                    {
                        existingPrimary.IsPrimary = false;
                        existingPrimary.LineType = LineType.Secondary;
                    }
                }

                phone.IndexNumber = newIndexNumber;
                phone.AssignedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Phone {PhoneNumber} reassigned from {OldIndex} to {NewIndex} during EOS processing",
                    phone.PhoneNumber, oldIndexNumber, newIndexNumber);

                return new JsonResult(new
                {
                    success = true,
                    message = $"Phone {phone.PhoneNumber} reassigned successfully to {newUser.FirstName} {newUser.LastName}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reassigning phone {PhoneId}", phoneId);
                return new JsonResult(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Toggle User Profile - Enable or disable user account during EOS processing
        /// </summary>
        public async Task<JsonResult> OnPostToggleUserProfileAsync(string indexNumber)
        {
            try
            {
                var user = await _context.EbillUsers.FirstOrDefaultAsync(u => u.IndexNumber == indexNumber);

                if (user == null)
                {
                    return new JsonResult(new { success = false, message = "User not found" });
                }

                user.IsActive = !user.IsActive;
                var action = user.IsActive ? "enabled" : "disabled";

                await _context.SaveChangesAsync();

                _logger.LogInformation("User profile {IndexNumber} {Action} during EOS processing by {User}",
                    indexNumber, action, User.Identity?.Name);

                return new JsonResult(new
                {
                    success = true,
                    message = $"User profile {action} successfully",
                    isActive = user.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user profile {IndexNumber}", indexNumber);
                return new JsonResult(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Deactivate Phone - Set phone status to Deactivated for EOS staff
        /// </summary>
        public async Task<JsonResult> OnPostDeactivatePhoneAsync(int phoneId)
        {
            try
            {
                var phone = await _context.UserPhones.FindAsync(phoneId);

                if (phone == null)
                {
                    return new JsonResult(new { success = false, message = "Phone record not found" });
                }

                // Do not deactivate fixed line numbers (Desk, Extension, Conference, Fax)
                if (phone.PhoneType != PhoneTypes.Mobile && phone.PhoneType != PhoneTypes.Home && phone.PhoneType != PhoneTypes.Temporary)
                {
                    return new JsonResult(new { success = false, message = $"Cannot deactivate a fixed line ({phone.PhoneType}). Only mobile, home, and temporary lines can be deactivated." });
                }

                phone.Status = PhoneStatus.Deactivated;
                phone.IsPrimary = false;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Phone {PhoneNumber} deactivated during EOS processing for {IndexNumber}",
                    phone.PhoneNumber, phone.IndexNumber);

                return new JsonResult(new
                {
                    success = true,
                    message = $"Phone {phone.PhoneNumber} deactivated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating phone {PhoneId}", phoneId);
                return new JsonResult(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // DTOs
        public class EOSStaffRecovery
        {
            public string IndexNumber { get; set; } = string.Empty;
            public string StaffName { get; set; } = string.Empty;
            public string? Email { get; set; }
            public string? Organization { get; set; }
            public string BatchName { get; set; } = string.Empty;
            public DateTime BatchDate { get; set; }
            public int TotalRecords { get; set; }
            public decimal TotalPersonalAmount { get; set; }
            public decimal TotalOfficialAmount { get; set; }
            public decimal TotalRecoveryAmount { get; set; }
            public int ApprovedRecordsCount { get; set; }
            public bool PendingRecovery { get; set; }
            public string? VerificationStatus { get; set; }
            public DateTime? VerificationDeadline { get; set; }
            public string? SupervisorApprovalStatus { get; set; }
            public DateTime? SupervisorApprovalDeadline { get; set; }
        }

        public class EOSRecoveryStatistics
        {
            public int TotalEOSStaff { get; set; }
            public int TotalPendingRecords { get; set; }
            public decimal TotalPendingAmount { get; set; }
            public int TotalProcessedStaff { get; set; }
            public decimal TotalRecoveredAmount { get; set; }
            public int TotalRecoveredRecords { get; set; }
            public DateTime? LastProcessedDate { get; set; }
        }
    }
}
