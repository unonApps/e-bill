using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TAB.Web.Data;
using TAB.Web.Models;
using TAB.Web.Models.DTOs;

namespace TAB.Web.Services
{
    /// <summary>
    /// Service for generating call log recovery reports
    /// </summary>
    public class CallLogReportingService : ICallLogReportingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CallLogReportingService> _logger;

        public CallLogReportingService(
            ApplicationDbContext context,
            ILogger<CallLogReportingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<RecoverySummaryReport> GetRecoverySummaryAsync(ReportFilter filter)
        {
            try
            {
                var query = BuildRecoveryLogQuery(filter);
                var logs = await query.ToListAsync();

                var report = new RecoverySummaryReport
                {
                    TotalRecords = logs.Count,
                    TotalAmountRecovered = logs.Sum(l => l.AmountRecovered),
                    AppliedFilters = filter
                };

                // Calculate breakdowns
                report.PersonalRecovery = new RecoveryBreakdown
                {
                    Count = logs.Count(l => l.RecoveryAction == "Personal"),
                    Amount = logs.Where(l => l.RecoveryAction == "Personal").Sum(l => l.AmountRecovered)
                };

                report.OfficialRecovery = new RecoveryBreakdown
                {
                    Count = logs.Count(l => l.RecoveryAction == "Official"),
                    Amount = logs.Where(l => l.RecoveryAction == "Official").Sum(l => l.AmountRecovered)
                };

                report.ClassOfServiceRecovery = new RecoveryBreakdown
                {
                    Count = logs.Count(l => l.RecoveryAction == "ClassOfService"),
                    Amount = logs.Where(l => l.RecoveryAction == "ClassOfService").Sum(l => l.AmountRecovered)
                };

                // Calculate percentages
                if (report.TotalAmountRecovered > 0)
                {
                    report.PersonalRecovery.Percentage = (report.PersonalRecovery.Amount / report.TotalAmountRecovered) * 100;
                    report.OfficialRecovery.Percentage = (report.OfficialRecovery.Amount / report.TotalAmountRecovered) * 100;
                    report.ClassOfServiceRecovery.Percentage = (report.ClassOfServiceRecovery.Amount / report.TotalAmountRecovered) * 100;
                }

                // Recovery by type
                report.RecoveryByType = logs
                    .GroupBy(l => l.RecoveryType)
                    .Select(g => new RecoveryTypeBreakdown
                    {
                        Type = g.Key,
                        Count = g.Count(),
                        Amount = g.Sum(l => l.AmountRecovered),
                        Description = GetRecoveryTypeDescription(g.Key)
                    })
                    .OrderByDescending(r => r.Amount)
                    .ToList();

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating recovery summary report");
                throw;
            }
        }

        public async Task<List<StaffRecoveryDetail>> GetStaffRecoveryDetailsAsync(ReportFilter filter)
        {
            try
            {
                var query = BuildRecoveryLogQuery(filter);
                var logs = await query.ToListAsync();

                var staffRecovery = logs
                    .GroupBy(rl => rl.RecoveredFrom)
                    .Select(g => new StaffRecoveryDetail
                    {
                        IndexNumber = g.Key ?? "Unknown",
                        TotalCalls = g.Count(),
                        TotalAmount = g.Sum(rl => rl.AmountRecovered),
                        PersonalCalls = g.Count(rl => rl.RecoveryAction == "Personal"),
                        PersonalAmount = g.Where(rl => rl.RecoveryAction == "Personal").Sum(rl => rl.AmountRecovered),
                        OfficialCalls = g.Count(rl => rl.RecoveryAction == "Official"),
                        OfficialAmount = g.Where(rl => rl.RecoveryAction == "Official").Sum(rl => rl.AmountRecovered),
                        ClassOfServiceCalls = g.Count(rl => rl.RecoveryAction == "ClassOfService"),
                        ClassOfServiceAmount = g.Where(rl => rl.RecoveryAction == "ClassOfService").Sum(rl => rl.AmountRecovered),
                        MissedDeadlines = g.Count(rl => rl.RecoveryType == "StaffNonVerification" || rl.RecoveryType == "SupervisorRevertFailure"),
                        TimesReverted = g.Count(rl => rl.RecoveryType == "SupervisorRevertFailure")
                    })
                    .ToList();

                // Enrich with user details
                foreach (var detail in staffRecovery)
                {
                    var user = await _context.EbillUsers.FirstOrDefaultAsync(u => u.IndexNumber == detail.IndexNumber);
                    if (user != null)
                    {
                        detail.FirstName = user.FirstName;
                        detail.LastName = user.LastName;
                        detail.Email = user.Email;
                        // Note: Department and Office would need to be added to EbillUser model or fetched separately
                    }

                    // Calculate compliance rate
                    var totalDeadlines = detail.MissedDeadlines + detail.TotalCalls;
                    if (totalDeadlines > 0)
                    {
                        detail.ComplianceRate = ((decimal)(totalDeadlines - detail.MissedDeadlines) / totalDeadlines) * 100;
                    }
                }

                // Apply pagination
                var skip = (filter.PageNumber - 1) * filter.PageSize;
                return staffRecovery
                    .OrderByDescending(s => s.TotalAmount)
                    .Skip(skip)
                    .Take(filter.PageSize)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating staff recovery details report");
                throw;
            }
        }

        public async Task<List<SupervisorActivityReport>> GetSupervisorActivityAsync(ReportFilter filter)
        {
            try
            {
                // Get all verifications within the filter period
                var query = _context.CallLogVerifications
                    .Include(v => v.CallRecord)
                    .AsQueryable();

                if (filter.StartDate.HasValue)
                    query = query.Where(v => v.CreatedDate >= filter.StartDate.Value);
                if (filter.EndDate.HasValue)
                    query = query.Where(v => v.CreatedDate <= filter.EndDate.Value);
                if (!string.IsNullOrEmpty(filter.IndexNumber))
                    query = query.Where(v => v.SupervisorIndexNumber == filter.IndexNumber);

                var verifications = await query.ToListAsync();

                var supervisorActivity = verifications
                    .Where(v => !string.IsNullOrEmpty(v.SupervisorIndexNumber))
                    .GroupBy(v => v.SupervisorIndexNumber)
                    .Select(g => new SupervisorActivityReport
                    {
                        SupervisorIndexNumber = g.Key!,
                        TotalSubmissionsReceived = g.Count(),
                        SubmissionsApproved = g.Count(v => v.SupervisorApprovalStatus == "Approved"),
                        SubmissionsPartiallyApproved = g.Count(v => v.SupervisorApprovalStatus == "PartiallyApproved"),
                        SubmissionsRejected = g.Count(v => v.SupervisorApprovalStatus == "Rejected"),
                        SubmissionsReverted = g.Count(v => v.SupervisorApprovalStatus == "Reverted"),
                        MissedDeadlines = g.Count(v => v.DeadlineMissed),
                        TotalAmountReviewed = g.Sum(v => v.ActualAmount),
                        AmountApproved = g.Where(v => v.SupervisorApprovalStatus == "Approved" || v.SupervisorApprovalStatus == "PartiallyApproved")
                            .Sum(v => v.ApprovedAmount ?? v.ActualAmount),
                        StaffSupervised = g.Select(v => v.VerifiedBy).Distinct().Count()
                    })
                    .ToList();

                // Enrich with user details and calculate metrics
                foreach (var activity in supervisorActivity)
                {
                    var user = await _context.EbillUsers.FirstOrDefaultAsync(u => u.IndexNumber == activity.SupervisorIndexNumber);
                    if (user != null)
                    {
                        activity.SupervisorName = $"{user.FirstName} {user.LastName}";
                        activity.Email = user.Email;
                    }

                    // Calculate approval rate
                    if (activity.TotalSubmissionsReceived > 0)
                    {
                        activity.ApprovalRate = ((decimal)(activity.SubmissionsApproved + activity.SubmissionsPartiallyApproved) / activity.TotalSubmissionsReceived) * 100;
                    }

                    // Calculate average response time
                    var supervisorVerifications = verifications.Where(v => v.SupervisorIndexNumber == activity.SupervisorIndexNumber && v.SupervisorApprovedDate != null).ToList();
                    if (supervisorVerifications.Any())
                    {
                        activity.AverageResponseTimeHours = supervisorVerifications
                            .Average(v => (v.SupervisorApprovedDate!.Value - v.SubmittedDate!.Value).TotalHours);
                    }
                }

                return supervisorActivity.OrderByDescending(s => s.TotalSubmissionsReceived).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating supervisor activity report");
                throw;
            }
        }

        public async Task<List<BatchAnalysisReport>> GetBatchAnalysisAsync(ReportFilter filter)
        {
            try
            {
                var query = _context.StagingBatches.AsQueryable();

                if (filter.StartDate.HasValue)
                    query = query.Where(b => b.CreatedDate >= filter.StartDate.Value);
                if (filter.EndDate.HasValue)
                    query = query.Where(b => b.CreatedDate <= filter.EndDate.Value);
                if (filter.BatchId.HasValue)
                    query = query.Where(b => b.Id == filter.BatchId.Value);

                var batches = await query.ToListAsync();

                var reports = new List<BatchAnalysisReport>();

                foreach (var batch in batches)
                {
                    var recoveryLogs = await _context.RecoveryLogs
                        .Where(rl => rl.BatchId == batch.Id)
                        .ToListAsync();

                    var report = new BatchAnalysisReport
                    {
                        BatchId = batch.Id,
                        BatchName = batch.BatchName,
                        CreatedDate = batch.CreatedDate,
                        TotalAmount = batch.TotalRecoveredAmount ?? 0,
                        TotalCalls = recoveryLogs.Count,
                        PersonalRecoveryCalls = recoveryLogs.Count(l => l.RecoveryAction == "Personal"),
                        PersonalRecoveryAmount = recoveryLogs.Where(l => l.RecoveryAction == "Personal").Sum(l => l.AmountRecovered),
                        OfficialRecoveryCalls = recoveryLogs.Count(l => l.RecoveryAction == "Official"),
                        OfficialRecoveryAmount = recoveryLogs.Where(l => l.RecoveryAction == "Official").Sum(l => l.AmountRecovered),
                        ClassOfServiceCalls = recoveryLogs.Count(l => l.RecoveryAction == "ClassOfService"),
                        ClassOfServiceAmount = recoveryLogs.Where(l => l.RecoveryAction == "ClassOfService").Sum(l => l.AmountRecovered)
                    };

                    // Calculate compliance metrics
                    var staffWithCalls = recoveryLogs.Select(l => l.RecoveredFrom).Distinct().Count();
                    var staffWhoMissed = recoveryLogs.Where(l => l.RecoveryType == "StaffNonVerification").Select(l => l.RecoveredFrom).Distinct().Count();
                    report.StaffWhoVerified = staffWithCalls - staffWhoMissed;
                    report.StaffWhoMissedDeadline = staffWhoMissed;
                    if (staffWithCalls > 0)
                    {
                        report.StaffComplianceRate = ((decimal)report.StaffWhoVerified / staffWithCalls) * 100;
                    }

                    reports.Add(report);
                }

                return reports;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating batch analysis report");
                throw;
            }
        }

        public async Task<(List<RecoveryLog> Logs, int TotalCount)> GetRecoveryLogsAsync(ReportFilter filter)
        {
            try
            {
                var query = BuildRecoveryLogQuery(filter);

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var skip = (filter.PageNumber - 1) * filter.PageSize;
                var logs = await query
                    .OrderByDescending(rl => rl.RecoveryDate)
                    .Skip(skip)
                    .Take(filter.PageSize)
                    .Include(rl => rl.CallRecord)
                    .Include(rl => rl.StagingBatch)
                    .ToListAsync();

                return (logs, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recovery logs");
                throw;
            }
        }

        public async Task<byte[]> ExportToExcelAsync(string reportType, ReportFilter filter)
        {
            // TODO: Implement Excel export using EPPlus or ClosedXML
            // This would require adding the appropriate NuGet package
            _logger.LogWarning("Excel export not yet implemented");
            throw new NotImplementedException("Excel export functionality will be implemented in a future update");
        }

        public async Task<List<RecoveryTrendData>> GetRecoveryTrendDataAsync(DateTime startDate, DateTime endDate, string groupBy = "month")
        {
            try
            {
                var logs = await _context.RecoveryLogs
                    .Where(rl => rl.RecoveryDate >= startDate && rl.RecoveryDate <= endDate)
                    .ToListAsync();

                List<RecoveryTrendData> trendData;

                if (groupBy.ToLower() == "week")
                {
                    trendData = logs
                        .GroupBy(l => new
                        {
                            Year = l.RecoveryDate.Year,
                            Week = System.Globalization.ISOWeek.GetWeekOfYear(l.RecoveryDate)
                        })
                        .Select(g => new RecoveryTrendData
                        {
                            Period = $"{g.Key.Year}-W{g.Key.Week:00}",
                            PeriodStart = System.Globalization.ISOWeek.ToDateTime(g.Key.Year, g.Key.Week, DayOfWeek.Monday),
                            PeriodEnd = System.Globalization.ISOWeek.ToDateTime(g.Key.Year, g.Key.Week, DayOfWeek.Monday).AddDays(6),
                            PersonalAmount = g.Where(l => l.RecoveryAction == "Personal").Sum(l => l.AmountRecovered),
                            OfficialAmount = g.Where(l => l.RecoveryAction == "Official").Sum(l => l.AmountRecovered),
                            ClassOfServiceAmount = g.Where(l => l.RecoveryAction == "ClassOfService").Sum(l => l.AmountRecovered),
                            TotalCalls = g.Count()
                        })
                        .OrderBy(t => t.PeriodStart)
                        .ToList();
                }
                else // month
                {
                    trendData = logs
                        .GroupBy(l => new { l.RecoveryDate.Year, l.RecoveryDate.Month })
                        .Select(g => new RecoveryTrendData
                        {
                            Period = $"{g.Key.Year}-{g.Key.Month:00}",
                            PeriodStart = new DateTime(g.Key.Year, g.Key.Month, 1),
                            PeriodEnd = new DateTime(g.Key.Year, g.Key.Month, DateTime.DaysInMonth(g.Key.Year, g.Key.Month)),
                            PersonalAmount = g.Where(l => l.RecoveryAction == "Personal").Sum(l => l.AmountRecovered),
                            OfficialAmount = g.Where(l => l.RecoveryAction == "Official").Sum(l => l.AmountRecovered),
                            ClassOfServiceAmount = g.Where(l => l.RecoveryAction == "ClassOfService").Sum(l => l.AmountRecovered),
                            TotalCalls = g.Count()
                        })
                        .OrderBy(t => t.PeriodStart)
                        .ToList();
                }

                return trendData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating recovery trend data");
                throw;
            }
        }

        public async Task<List<StaffRecoveryDetail>> GetTopStaffByPersonalRecoveryAsync(int topCount = 10, DateTime? startDate = null, DateTime? endDate = null)
        {
            var filter = new ReportFilter
            {
                StartDate = startDate,
                EndDate = endDate,
                RecoveryAction = "Personal",
                PageSize = topCount
            };

            var staffDetails = await GetStaffRecoveryDetailsAsync(filter);
            return staffDetails.OrderByDescending(s => s.PersonalAmount).Take(topCount).ToList();
        }

        public async Task<List<DepartmentRecoverySummary>> GetDepartmentRecoverySummaryAsync(ReportFilter filter)
        {
            // This would require Department information in EbillUser or CallRecord
            // Placeholder implementation
            _logger.LogWarning("Department recovery summary not yet fully implemented - requires department data");
            return new List<DepartmentRecoverySummary>();
        }

        public async Task<ComplianceMetrics> GetComplianceMetricsAsync(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var query = _context.RecoveryLogs.AsQueryable();

                if (startDate.HasValue)
                    query = query.Where(rl => rl.RecoveryDate >= startDate.Value);
                if (endDate.HasValue)
                    query = query.Where(rl => rl.RecoveryDate <= endDate.Value);

                var logs = await query.ToListAsync();

                var metrics = new ComplianceMetrics
                {
                    TotalVerifications = logs.Count,
                    VerificationsMissedDeadline = logs.Count(l => l.RecoveryType == "StaffNonVerification"),
                    TotalApprovals = logs.Count(l => l.RecoveryType == "SupervisorNonApproval" || l.RecoveryType == "SupervisorPartialApproval"),
                    ApprovalsMissedDeadline = logs.Count(l => l.RecoveryType == "SupervisorNonApproval"),
                    TotalReverts = logs.Count(l => l.RecoveryType == "SupervisorRevertFailure")
                };

                metrics.VerificationsOnTime = metrics.TotalVerifications - metrics.VerificationsMissedDeadline;
                metrics.ApprovalsOnTime = metrics.TotalApprovals - metrics.ApprovalsMissedDeadline;

                if (metrics.TotalVerifications > 0)
                {
                    metrics.StaffComplianceRate = ((decimal)metrics.VerificationsOnTime / metrics.TotalVerifications) * 100;
                }

                if (metrics.TotalApprovals > 0)
                {
                    metrics.SupervisorComplianceRate = ((decimal)metrics.ApprovalsOnTime / metrics.TotalApprovals) * 100;
                }

                var totalActions = metrics.TotalVerifications + metrics.TotalApprovals;
                if (totalActions > 0)
                {
                    metrics.OverallComplianceRate = ((decimal)(metrics.VerificationsOnTime + metrics.ApprovalsOnTime) / totalActions) * 100;
                }

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating compliance metrics");
                throw;
            }
        }

        #region Private Helper Methods

        private IQueryable<RecoveryLog> BuildRecoveryLogQuery(ReportFilter filter)
        {
            var query = _context.RecoveryLogs.AsQueryable();

            if (filter.StartDate.HasValue)
                query = query.Where(rl => rl.RecoveryDate >= filter.StartDate.Value);
            if (filter.EndDate.HasValue)
                query = query.Where(rl => rl.RecoveryDate <= filter.EndDate.Value);
            if (!string.IsNullOrEmpty(filter.IndexNumber))
                query = query.Where(rl => rl.RecoveredFrom == filter.IndexNumber);
            if (filter.BatchId.HasValue)
                query = query.Where(rl => rl.BatchId == filter.BatchId.Value);
            if (!string.IsNullOrEmpty(filter.RecoveryType))
                query = query.Where(rl => rl.RecoveryType == filter.RecoveryType);
            if (!string.IsNullOrEmpty(filter.RecoveryAction))
                query = query.Where(rl => rl.RecoveryAction == filter.RecoveryAction);

            return query;
        }

        private string GetRecoveryTypeDescription(string recoveryType)
        {
            return recoveryType switch
            {
                "StaffNonVerification" => "Staff failed to verify within deadline",
                "SupervisorNonApproval" => "Supervisor failed to approve within deadline",
                "SupervisorPartialApproval" => "Supervisor partially approved verification",
                "SupervisorRejection" => "Supervisor rejected verification",
                "SupervisorRevertFailure" => "Staff failed to re-verify after supervisor revert",
                "ManualOverride" => "Manual recovery override by administrator",
                _ => "Unknown recovery type"
            };
        }

        #endregion
    }
}
