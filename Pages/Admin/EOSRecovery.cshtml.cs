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
            // Declare failedStaff outside try block so it's accessible in catch block
            var failedStaff = new List<(string IndexNumber, string StaffName, string Error)>();

            // Use database transaction for atomic operation
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

                // Generate unique execution ID to track this specific recovery operation
                var executionId = Guid.NewGuid();
                var executionTime = DateTime.UtcNow;

                int totalProcessed = 0;
                int totalSuccess = 0;
                int totalFailed = 0;
                decimal totalRecovered = 0;

                // Optimize: Get ONLY PERSONAL approved records in ONE query
                // NOTE: Official calls are already certified as official business - NO recovery needed
                var allApprovedRecords = await _context.CallRecords
                    .Where(r => SelectedStaffIndexNumbers.Contains(r.ResponsibleIndexNumber) &&
                               r.SupervisorApprovalStatus == "Approved" &&
                               r.VerificationType == "Personal" && // ONLY Personal calls need recovery
                               (r.RecoveryStatus == "Pending" || r.RecoveryStatus == "NotProcessed"))
                    .ToListAsync();

                if (!allApprovedRecords.Any())
                {
                    await transaction.RollbackAsync();
                    ErrorMessage = "No Personal calls found for recovery. The selected staff members only have Official calls which are already certified as official business.";
                    await LoadEOSStaffDataAsync();
                    await LoadStatisticsAsync();
                    return Page();
                }

                // Group records by staff member
                var recordsByStaff = allApprovedRecords.GroupBy(r => r.ResponsibleIndexNumber);

                foreach (var staffGroup in recordsByStaff)
                {
                    var indexNumber = staffGroup.Key;
                    var approvedRecords = staffGroup.ToList();

                    // Get staff info for error reporting
                    var staffInfo = await _context.EbillUsers
                        .Where(u => u.IndexNumber == indexNumber)
                        .Select(u => new { u.FullName })
                        .FirstOrDefaultAsync();
                    var staffName = staffInfo?.FullName ?? indexNumber;

                    try
                    {
                        if (!approvedRecords.Any())
                        {
                            _logger.LogWarning("No approved records found for EOS staff {IndexNumber}", indexNumber);
                            continue;
                        }

                        // Process recovery for each PERSONAL record
                        // NOTE: At this point, all records are Personal (filtered in query above)
                        foreach (var record in approvedRecords)
                        {
                            totalProcessed++;

                            try
                            {
                                // Verify this is a Personal call (should always be true due to query filter)
                                if (record.VerificationType != "Personal")
                                {
                                    _logger.LogWarning("Skipping non-Personal record {RecordId} - VerificationType: {Type}",
                                        record.Id, record.VerificationType);
                                    continue;
                                }

                                // Recovery logic for Personal calls ONLY
                                string finalAssignmentType = "Personal";
                                decimal recoveryAmount = record.CallCostUSD;

                                // Update call record fields
                                record.AssignmentStatus = "Personal";
                                record.FinalAssignmentType = "Personal";

                                // Create recovery log for Personal call recovery
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
                                        ExecutionId = executionId.ToString(), // Track this execution
                                        ExecutionTime = executionTime
                                    })
                                };

                                _context.RecoveryLogs.Add(recoveryLog);
                                // Don't save yet - will save all at once outside loop

                                // Update call record status
                                record.RecoveryStatus = "Completed";
                                record.RecoveryAmount = recoveryAmount;
                                record.RecoveryDate = DateTime.UtcNow;
                                record.RecoveryProcessedBy = User.Identity?.Name ?? "System";

                                // Count Personal call towards total recovered
                                totalRecovered += recoveryAmount;
                                totalSuccess++;

                                _logger.LogInformation("EOS Recovery processed for Personal record {RecordId}, Amount: ${Amount:F2}",
                                    record.Id, recoveryAmount);
                            }
                            catch (Exception recordEx)
                            {
                                totalFailed++;
                                _logger.LogError(recordEx, "Failed to process recovery for record {RecordId}", record.Id);
                                throw; // Re-throw to be caught by outer catch and rollback transaction
                            }
                        }

                        // Don't save changes here - will save once at the end
                    }
                    catch (Exception staffEx)
                    {
                        _logger.LogError(staffEx, "Failed to process recovery for staff {IndexNumber}", indexNumber);
                        failedStaff.Add((indexNumber, staffName, staffEx.Message));
                        totalFailed++;

                        // For critical errors, rollback and stop
                        await transaction.RollbackAsync();
                        throw;
                    }
                }

                // Save all changes at once (outside the loop)
                await _context.SaveChangesAsync();

                // Update batch totals (same pattern as CallLogRecoveryService)
                // FIX: Only use recovery logs from THIS execution (using executionId)
                var processedBatchIds = await _context.CallRecords
                    .Where(r => SelectedStaffIndexNumbers.Contains(r.ResponsibleIndexNumber) &&
                               r.SourceBatchId.HasValue &&
                               r.RecoveryStatus == "Completed")
                    .Select(r => r.SourceBatchId!.Value)
                    .Distinct()
                    .ToListAsync();

                var batchesToUpdate = await _context.StagingBatches
                    .Where(b => processedBatchIds.Contains(b.Id))
                    .ToListAsync();

                // Use executionId string for batch queries
                var executionIdString = executionId.ToString();

                foreach (var batch in batchesToUpdate)
                {
                    // Calculate Personal amounts for THIS EXECUTION ONLY
                    // NOTE: We only process Personal calls, so officialAmount will always be 0
                    var batchRecoveryLogs = await _context.RecoveryLogs
                        .Where(r => r.BatchId == batch.Id &&
                                   r.RecoveryType == "EOS" &&
                                   r.Metadata.Contains(executionIdString)) // Only this execution
                        .ToListAsync();

                    var personalAmount = batchRecoveryLogs
                        .Where(r => r.RecoveryAction == "Personal")
                        .Sum(r => r.AmountRecovered);

                    // Update batch totals (same as CallLogRecoveryService)
                    // NOTE: We only process Personal calls, so only Personal totals are updated
                    batch.TotalPersonalAmount = (batch.TotalPersonalAmount ?? 0) + personalAmount;
                    batch.TotalRecoveredAmount = (batch.TotalRecoveredAmount ?? 0) + personalAmount; // Only Personal is "recovered"

                    // Check if all records in this batch have been processed
                    var allRecordsRecovered = !await _context.CallRecords
                        .AnyAsync(r => r.SourceBatchId == batch.Id &&
                                      r.SupervisorApprovalStatus == "Approved" &&
                                      (r.RecoveryStatus == "Pending" || r.RecoveryStatus == "NotProcessed"));

                    if (allRecordsRecovered)
                    {
                        batch.RecoveryStatus = "Completed";
                        batch.RecoveryProcessingDate = DateTime.UtcNow;
                        _logger.LogInformation("Batch {BatchId} marked as recovery completed - Personal: ${Personal:F2}",
                            batch.Id, personalAmount);
                    }
                    else
                    {
                        batch.RecoveryStatus = "InProgress";
                        _logger.LogInformation("Batch {BatchId} marked as recovery in progress", batch.Id);
                    }
                }

                await _context.SaveChangesAsync();

                // Commit transaction - all changes successful
                await transaction.CommitAsync();

                // Count Personal records processed (should be same as totalSuccess)
                // NOTE: We only process Personal calls, Official calls are not touched
                var personalRecordsProcessed = await _context.RecoveryLogs
                    .Where(r => r.RecoveryType == "EOS" &&
                               r.Metadata.Contains(executionIdString) &&
                               r.RecoveryAction == "Personal")
                    .CountAsync();

                // Build success message with detailed information
                var successDetails = $"EOS Recovery completed! " +
                    $"Processed: {personalRecordsProcessed} Personal call(s) recovered. " +
                    $"Success: {totalSuccess}, Failed: {totalFailed}. " +
                    $"Total Recovered: ${totalRecovered:F2}";

                // Add failed staff details if any
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
                // Transaction will be automatically rolled back
                _logger.LogError(ex, "Error triggering EOS recovery - transaction rolled back");

                // Build detailed error message
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
            // Business Logic: Query CallRecords directly and join to INTERIM batches
            // Show records where:
            // 1. SourceBatchId links to a batch with BatchCategory = "INTERIM"
            // 2. Batch is Published (BatchStatus = Published)
            // 3. Staff has verified the record (IsVerified = true)
            // 4. Supervisor has approved OR pending approval
            // 5. Recovery is not completed yet
            //
            // Recovery Rules (same as regular recovery):
            // - Personal calls: Full recovery from staff
            // - Official calls: NO recovery (certified as official business)

            var allStaffList = new List<EOSStaffRecovery>();

            // Get all staff with INTERIM batch records
            _logger.LogInformation("Starting EOS Recovery data load...");

            // Debug: Check all INTERIM batches
            var allInterimBatches = await _context.StagingBatches
                .Where(b => b.BatchCategory == "INTERIM")
                .ToListAsync();
            _logger.LogInformation($"Found {allInterimBatches.Count} INTERIM batches");

            // Debug: Check published INTERIM batches
            var publishedInterimBatches = allInterimBatches.Where(b => b.BatchStatus == BatchStatus.Published).ToList();
            _logger.LogInformation($"Found {publishedInterimBatches.Count} Published INTERIM batches");

            var staffWithInterimRecords = await _context.CallRecords
                .Where(r => r.SourceBatchId.HasValue &&
                           r.IsVerified == true &&
                           (r.SupervisorApprovalStatus == "Approved" || r.SupervisorApprovalStatus == "Pending") &&
                           (r.RecoveryStatus == "Pending" || r.RecoveryStatus == "NotProcessed"))
                .Join(_context.StagingBatches,
                     cr => cr.SourceBatchId,
                     sb => (Guid?)sb.Id,
                     (cr, sb) => new { CallRecord = cr, Batch = sb })
                .Where(x => x.Batch.BatchCategory == "INTERIM" &&
                           x.Batch.BatchStatus == BatchStatus.Published)
                .Select(x => x.CallRecord.ResponsibleIndexNumber)
                .Distinct()
                .ToListAsync();

            _logger.LogInformation($"Found {staffWithInterimRecords.Count} staff with INTERIM records");

            foreach (var indexNumber in staffWithInterimRecords)
            {
                if (string.IsNullOrEmpty(indexNumber))
                    continue;

                // Get verified records from INTERIM batches for this staff member
                var query = _context.CallRecords
                    .Where(r => r.ResponsibleIndexNumber == indexNumber &&
                               r.SourceBatchId.HasValue &&
                               r.IsVerified == true &&
                               (r.SupervisorApprovalStatus == "Approved" || r.SupervisorApprovalStatus == "Pending") &&
                               (r.RecoveryStatus == "Pending" || r.RecoveryStatus == "NotProcessed"))
                    .Join(_context.StagingBatches,
                         cr => cr.SourceBatchId,
                         sb => (Guid?)sb.Id,
                         (cr, sb) => new { CallRecord = cr, Batch = sb })
                    .Where(x => x.Batch.BatchCategory == "INTERIM" &&
                               x.Batch.BatchStatus == BatchStatus.Published)
                    .Select(x => x.CallRecord);

                // Apply month filter if specified
                if (FilterMonth.HasValue)
                {
                    query = query.Where(r => r.CallMonth == FilterMonth.Value);
                }

                // Apply year filter if specified
                if (FilterYear.HasValue)
                {
                    query = query.Where(r => r.CallYear == FilterYear.Value);
                }

                var verifiedRecords = await query.ToListAsync();

                _logger.LogInformation($"Staff {indexNumber}: Found {verifiedRecords.Count} verified records");

                if (!verifiedRecords.Any())
                    continue; // Skip staff with no verified records pending recovery

                // Get staff information
                var staff = await _context.EbillUsers
                    .Include(u => u.OrganizationEntity)
                    .FirstOrDefaultAsync(u => u.IndexNumber == indexNumber);

                if (staff == null)
                    continue;

                // Calculate amounts per verification type
                var totalPersonal = verifiedRecords
                    .Where(r => r.VerificationType == "Personal")
                    .Sum(r => r.CallCostUSD);

                var totalOfficial = verifiedRecords
                    .Where(r => r.VerificationType == "Official")
                    .Sum(r => r.CallCostUSD);

                // Total recovery amount = Only Personal calls (Official calls are NOT recovered)
                var totalAmount = totalPersonal;

                // Get latest batch info from the records
                var latestBatchId = verifiedRecords
                    .Where(r => r.SourceBatchId.HasValue)
                    .OrderByDescending(r => r.CallDate)
                    .Select(r => r.SourceBatchId)
                    .FirstOrDefault();

                var latestBatch = latestBatchId.HasValue
                    ? await _context.StagingBatches.FindAsync(latestBatchId.Value)
                    : null;

                // Get verification and approval information from the most recent batch
                var recentRecord = verifiedRecords.OrderByDescending(r => r.CallDate).FirstOrDefault();

                var staffRecovery = new EOSStaffRecovery
                {
                    IndexNumber = indexNumber,
                    StaffName = staff.FullName,
                    Email = staff.Email,
                    Organization = staff.OrganizationEntity?.Name ?? "N/A",
                    BatchName = latestBatch?.BatchName ?? "N/A",
                    BatchDate = latestBatch?.CreatedDate ?? DateTime.MinValue,
                    TotalRecords = verifiedRecords.Count,
                    TotalPersonalAmount = totalPersonal,
                    TotalOfficialAmount = totalOfficial,
                    TotalRecoveryAmount = totalAmount,
                    ApprovedRecordsCount = verifiedRecords.Count(r => r.SupervisorApprovalStatus == "Approved"),
                    PendingRecovery = true,
                    VerificationStatus = recentRecord?.IsVerified == true ? "Verified" : "Pending",
                    VerificationDeadline = recentRecord?.VerificationPeriod,
                    SupervisorApprovalStatus = recentRecord?.SupervisorApprovalStatus ?? "Pending",
                    SupervisorApprovalDeadline = recentRecord?.ApprovalPeriod
                };

                allStaffList.Add(staffRecovery);
                _logger.LogInformation($"Added {staff.FullName} to EOS Recovery list with {verifiedRecords.Count} records totaling ${totalAmount:F2}");
            }

            _logger.LogInformation($"Total staff in EOS Recovery list before filtering: {allStaffList.Count}");

            // Get unique organizations for filter dropdown
            Organizations = allStaffList
                .Select(s => s.Organization)
                .Where(o => !string.IsNullOrEmpty(o) && o != "N/A")
                .Select(o => o!) // Not-null assertion after null check
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
                _ => allStaffList.OrderByDescending(s => s.TotalRecoveryAmount).ToList() // Default: amount
            };

            // Calculate pagination
            TotalRecords = allStaffList.Count;
            TotalPages = (int)Math.Ceiling(TotalRecords / (double)PageSize);

            // Ensure CurrentPage is valid
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
            // Get all EOS recovery logs
            var eosLogs = await _context.RecoveryLogs
                .Where(r => r.RecoveryType == "EOS")
                .ToListAsync();

            Statistics = new EOSRecoveryStatistics
            {
                TotalEOSStaff = EOSStaffList.Count,
                TotalPendingRecords = EOSStaffList.Sum(s => s.TotalRecords),
                TotalPendingAmount = EOSStaffList.Sum(s => s.TotalRecoveryAmount),
                TotalProcessedStaff = eosLogs.Select(l => l.RecoveredFrom).Distinct().Count(),
                TotalRecoveredAmount = eosLogs.Sum(l => l.AmountRecovered),
                TotalRecoveredRecords = eosLogs.Count(),
                LastProcessedDate = eosLogs.Any() ? eosLogs.Max(l => l.RecoveryDate) : (DateTime?)null
            };
        }

        /// <summary>
        /// Get EOS Profile - Returns user profile and assigned phone numbers for EOS processing
        /// </summary>
        public async Task<JsonResult> OnGetEOSProfileAsync(string indexNumber)
        {
            try
            {
                // Get user profile
                var user = await _context.EbillUsers
                    .Include(u => u.OrganizationEntity)
                    .FirstOrDefaultAsync(u => u.IndexNumber == indexNumber);

                if (user == null)
                {
                    return new JsonResult(new { success = false, message = "User not found" });
                }

                // Get assigned phone numbers
                var phones = await _context.UserPhones
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

                var result = new
                {
                    success = true,
                    indexNumber = user.IndexNumber,
                    fullName = $"{user.FirstName} {user.LastName}",
                    email = user.Email,
                    organization = user.OrganizationEntity?.Name,
                    isActive = user.IsActive,
                    phones = phones
                };

                return new JsonResult(result);
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
                    .Include(u => u.OrganizationEntity)
                    .Where(u =>
                        u.FirstName.ToLower().Contains(query) ||
                        u.LastName.ToLower().Contains(query) ||
                        u.Email.ToLower().Contains(query) ||
                        u.IndexNumber.ToLower().Contains(query))
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .Take(10) // Limit to 10 results
                    .Select(u => new
                    {
                        indexNumber = u.IndexNumber,
                        fullName = $"{u.FirstName} {u.LastName}",
                        email = u.Email,
                        organization = u.OrganizationEntity != null ? u.OrganizationEntity.Name : null
                    })
                    .ToListAsync();

                return new JsonResult(new
                {
                    success = true,
                    results = results
                });
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
                // Get the phone record
                var phone = await _context.UserPhones
                    .Include(p => p.EbillUser)
                    .FirstOrDefaultAsync(p => p.Id == phoneId);

                if (phone == null)
                {
                    return new JsonResult(new { success = false, message = "Phone record not found" });
                }

                // Verify new staff member exists
                var newUser = await _context.EbillUsers
                    .FirstOrDefaultAsync(u => u.IndexNumber == newIndexNumber);

                if (newUser == null)
                {
                    return new JsonResult(new { success = false, message = "New staff member not found" });
                }

                // Store old values for logging
                var oldIndexNumber = phone.IndexNumber;
                var oldUserName = phone.EbillUser != null ? $"{phone.EbillUser.FirstName} {phone.EbillUser.LastName}" : oldIndexNumber;

                // Check if new user already has a primary phone if this is a primary phone
                if (phone.IsPrimary)
                {
                    var existingPrimary = await _context.UserPhones
                        .FirstOrDefaultAsync(p => p.IndexNumber == newIndexNumber && p.IsPrimary && p.IsActive);

                    if (existingPrimary != null)
                    {
                        // Set existing primary to secondary
                        existingPrimary.IsPrimary = false;
                        existingPrimary.LineType = LineType.Secondary;
                    }
                }

                // Reassign the phone
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

                // Toggle the IsActive status
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

                // Set status to Deactivated
                phone.Status = PhoneStatus.Deactivated;
                phone.IsPrimary = false; // Remove primary status when deactivating

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

            // Verification & Approval Information
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
