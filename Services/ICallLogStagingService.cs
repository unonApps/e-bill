using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TAB.Web.Models;

namespace TAB.Web.Services
{
    public interface ICallLogStagingService
    {
        // Consolidation
        Task<StagingBatch> ConsolidateCallLogsAsync(DateTime startDate, DateTime endDate, string createdBy);
        Task<int> ImportFromSafaricomAsync(Guid batchId, DateTime startDate, DateTime endDate);
        Task<int> ImportFromAirtelAsync(Guid batchId, DateTime startDate, DateTime endDate);
        Task<int> ImportFromPSTNAsync(Guid batchId, DateTime startDate, DateTime endDate);
        Task<int> ImportFromPrivateWireAsync(Guid batchId, DateTime startDate, DateTime endDate);

        // Anomaly Detection
        Task<List<CallLogAnomaly>> DetectAnomaliesAsync(int stagingId);
        Task<bool> ValidateCallLogAsync(CallLogStaging log);
        Task<int> DetectBatchAnomaliesAsync(Guid batchId);

        // Verification
        Task<bool> VerifyCallLogAsync(int stagingId, string verifiedBy, string? notes = null);
        Task<int> BulkVerifyAsync(List<int> stagingIds, string verifiedBy);
        Task<bool> RejectCallLogAsync(int stagingId, string rejectedBy, string reason);
        Task<int> BulkRejectAsync(List<int> stagingIds, string rejectedBy, string reason);

        // Production Push
        Task<int> PushToProductionAsync(Guid batchId, DateTime? verificationPeriod = null, string? verificationType = null);
        Task<bool> RollbackBatchAsync(Guid batchId);

        // Batch Management
        Task<bool> DeleteBatchAsync(Guid batchId, string deletedBy);
        Task<bool> CanDeleteBatchAsync(Guid batchId);

        // Queries
        Task<PagedResult<CallLogStaging>> GetStagedLogsAsync(StagingFilter filter);
        Task<StagingBatch?> GetBatchDetailsAsync(Guid batchId);
        Task<Dictionary<string, int>> GetBatchStatisticsAsync(Guid batchId);
        Task<List<StagingBatch>> GetRecentBatchesAsync(int count = 10);
        Task<bool> HasExistingBatchForPeriodAsync(int month, int year);
        Task<StagingBatch?> GetExistingBatchForPeriodAsync(int month, int year);
    }

    public class CallLogAnomaly
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public SeverityLevel Severity { get; set; }
        public Dictionary<string, object> Details { get; set; } = new();
    }

    public class StagingFilter
    {
        public Guid? BatchId { get; set; }
        public VerificationStatus? Status { get; set; }
        public bool? HasAnomalies { get; set; }
        public string? SearchTerm { get; set; }
        public string? ExtensionNumber { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string SortBy { get; set; } = "CallDate";
        public bool SortDescending { get; set; } = true;
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}