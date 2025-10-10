using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TAB.Web.Data;
using TAB.Web.Models;

namespace TAB.Web.Services
{
    public interface ICallLogCleanupService
    {
        Task<int> CleanupProcessedRecordsAsync(int daysToKeep = 30);
        Task<CleanupStatistics> GetCleanupStatisticsAsync();
    }

    public class CallLogCleanupService : ICallLogCleanupService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CallLogCleanupService> _logger;

        public CallLogCleanupService(ApplicationDbContext context, ILogger<CallLogCleanupService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Deletes processed call records older than specified days
        /// Should be called by a scheduled job (Hangfire, Quartz, etc.)
        /// </summary>
        public async Task<int> CleanupProcessedRecordsAsync(int daysToKeep = 30)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            var totalDeleted = 0;

            _logger.LogInformation("Starting cleanup of processed records older than {CutoffDate}", cutoffDate);

            try
            {
                // Clean Safaricom records
                var safaricomDeleted = await CleanupSafaricomAsync(cutoffDate);
                totalDeleted += safaricomDeleted;
                _logger.LogInformation("Deleted {Count} Safaricom records", safaricomDeleted);

                // Clean Airtel records
                var airtelDeleted = await CleanupAirtelAsync(cutoffDate);
                totalDeleted += airtelDeleted;
                _logger.LogInformation("Deleted {Count} Airtel records", airtelDeleted);

                // Clean PSTN records
                var pstnDeleted = await CleanupPSTNAsync(cutoffDate);
                totalDeleted += pstnDeleted;
                _logger.LogInformation("Deleted {Count} PSTN records", pstnDeleted);

                // Clean PrivateWire records
                var privateWireDeleted = await CleanupPrivateWireAsync(cutoffDate);
                totalDeleted += privateWireDeleted;
                _logger.LogInformation("Deleted {Count} PrivateWire records", privateWireDeleted);

                // Clean old staging records (90 days)
                var stagingDeleted = await CleanupStagingRecordsAsync(90);
                _logger.LogInformation("Deleted {Count} staging records", stagingDeleted);

                _logger.LogInformation("Cleanup completed. Total records deleted: {TotalDeleted}", totalDeleted);
                return totalDeleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup process");
                throw;
            }
        }

        private async Task<int> CleanupSafaricomAsync(DateTime cutoffDate)
        {
            const int batchSize = 1000;
            var totalDeleted = 0;

            while (true)
            {
                // Delete in batches to avoid locking
                var recordsToDelete = await _context.Safaricoms
                    .Where(s => s.ProcessingStatus == ProcessingStatus.Completed)
                    .Where(s => s.CreatedDate < cutoffDate) // Use CreatedDate since ProcessedDate doesn't exist
                    .Take(batchSize)
                    .ToListAsync();

                if (!recordsToDelete.Any())
                    break;

                _context.Safaricoms.RemoveRange(recordsToDelete);
                var deleted = await _context.SaveChangesAsync();
                totalDeleted += deleted;

                // Brief pause between batches
                await Task.Delay(100);
            }

            return totalDeleted;
        }

        private async Task<int> CleanupAirtelAsync(DateTime cutoffDate)
        {
            const int batchSize = 1000;
            var totalDeleted = 0;

            while (true)
            {
                var recordsToDelete = await _context.Airtels
                    .Where(a => a.ProcessingStatus == ProcessingStatus.Completed)
                    .Where(a => a.CreatedDate < cutoffDate)
                    .Take(batchSize)
                    .ToListAsync();

                if (!recordsToDelete.Any())
                    break;

                _context.Airtels.RemoveRange(recordsToDelete);
                var deleted = await _context.SaveChangesAsync();
                totalDeleted += deleted;

                await Task.Delay(100);
            }

            return totalDeleted;
        }

        private async Task<int> CleanupPSTNAsync(DateTime cutoffDate)
        {
            const int batchSize = 1000;
            var totalDeleted = 0;

            while (true)
            {
                var recordsToDelete = await _context.PSTNs
                    .Where(p => p.ProcessingStatus == ProcessingStatus.Completed)
                    .Where(p => p.CreatedDate < cutoffDate)
                    .Take(batchSize)
                    .ToListAsync();

                if (!recordsToDelete.Any())
                    break;

                _context.PSTNs.RemoveRange(recordsToDelete);
                var deleted = await _context.SaveChangesAsync();
                totalDeleted += deleted;

                await Task.Delay(100);
            }

            return totalDeleted;
        }

        private async Task<int> CleanupPrivateWireAsync(DateTime cutoffDate)
        {
            const int batchSize = 1000;
            var totalDeleted = 0;

            while (true)
            {
                var recordsToDelete = await _context.PrivateWires
                    .Where(p => p.ProcessingStatus == ProcessingStatus.Completed)
                    .Where(p => p.CreatedDate < cutoffDate)
                    .Take(batchSize)
                    .ToListAsync();

                if (!recordsToDelete.Any())
                    break;

                _context.PrivateWires.RemoveRange(recordsToDelete);
                var deleted = await _context.SaveChangesAsync();
                totalDeleted += deleted;

                await Task.Delay(100);
            }

            return totalDeleted;
        }

        private async Task<int> CleanupStagingRecordsAsync(int daysToKeep)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);

            // Delete old staging records that have been processed
            var stagingRecords = await _context.CallLogStagings
                .Where(c => c.ProcessingStatus == ProcessingStatus.Completed)
                .Where(c => c.ProcessedDate < cutoffDate)
                .ToListAsync();

            if (stagingRecords.Any())
            {
                _context.CallLogStagings.RemoveRange(stagingRecords);
                return await _context.SaveChangesAsync();
            }

            return 0;
        }

        public async Task<CleanupStatistics> GetCleanupStatisticsAsync()
        {
            var stats = new CleanupStatistics();

            // Count records by status
            stats.SafaricomStats = await GetTableStatisticsAsync("Safaricom");
            stats.AirtelStats = await GetTableStatisticsAsync("Airtel");
            stats.PSTNStats = await GetTableStatisticsAsync("PSTNs");
            stats.PrivateWireStats = await GetTableStatisticsAsync("PrivateWires");

            // Get staging statistics
            stats.StagingStats = new TableStatistics
            {
                TableName = "CallLogStagings",
                NewCount = await _context.CallLogStagings.CountAsync(c => c.ProcessingStatus == ProcessingStatus.Staged),
                ProcessedCount = await _context.CallLogStagings.CountAsync(c => c.ProcessingStatus == ProcessingStatus.Completed),
                TotalCount = await _context.CallLogStagings.CountAsync()
            };

            return stats;
        }

        private async Task<TableStatistics> GetTableStatisticsAsync(string tableName)
        {
            var stats = new TableStatistics { TableName = tableName };

            var sql = $@"
                SELECT
                    SUM(CASE WHEN ProcessingStatus = 0 THEN 1 ELSE 0 END) as NewCount,
                    SUM(CASE WHEN ProcessingStatus = 1 THEN 1 ELSE 0 END) as StagedCount,
                    SUM(CASE WHEN ProcessingStatus = 2 THEN 1 ELSE 0 END) as ProcessedCount,
                    COUNT(*) as TotalCount,
                    MIN(CreatedDate) as OldestRecord,
                    MAX(CreatedDate) as NewestRecord
                FROM {tableName}";

            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = sql;
                await _context.Database.OpenConnectionAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        stats.NewCount = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        stats.StagedCount = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                        stats.ProcessedCount = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                        stats.TotalCount = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);
                        stats.OldestRecord = reader.IsDBNull(4) ? null : reader.GetDateTime(4);
                        stats.NewestRecord = reader.IsDBNull(5) ? null : reader.GetDateTime(5);
                    }
                }
            }

            return stats;
        }
    }

    public class CleanupStatistics
    {
        public TableStatistics SafaricomStats { get; set; }
        public TableStatistics AirtelStats { get; set; }
        public TableStatistics PSTNStats { get; set; }
        public TableStatistics PrivateWireStats { get; set; }
        public TableStatistics StagingStats { get; set; }
    }

    public class TableStatistics
    {
        public string TableName { get; set; }
        public int NewCount { get; set; }
        public int StagedCount { get; set; }
        public int ProcessedCount { get; set; }
        public int TotalCount { get; set; }
        public DateTime? OldestRecord { get; set; }
        public DateTime? NewestRecord { get; set; }
    }
}