# Enterprise-Level Call Log Cleanup Solution

## Recommended Architecture

For enterprise production, use a **multi-layered approach** with redundancy and comprehensive monitoring:

```
┌─────────────────────────────────────────────────────────┐
│                   Enterprise Architecture                │
├─────────────────────────────────────────────────────────┤
│                                                         │
│  Primary:     SQL Server Agent Jobs (Database-level)   │
│  Secondary:   Hangfire (Application-level monitoring)   │
│  Monitoring:  Azure Monitor / Application Insights      │
│  Alerting:    Email + SMS + Teams/Slack                │
│  Audit:       Dedicated audit tables + Azure Log       │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

## Why This Combination?

### 1. **SQL Server Agent (Primary)**
- ✅ **Most Reliable** - Runs even if app is down
- ✅ **Best Performance** - Direct database operations
- ✅ **Built-in Scheduling** - No extra dependencies
- ✅ **Transaction Support** - Can rollback on errors
- ✅ **Native Monitoring** - SQL Server built-in alerts

### 2. **Hangfire (Secondary)**
- ✅ **Visual Dashboard** - Easy monitoring
- ✅ **Application Context** - Can send notifications
- ✅ **Retry Logic** - Automatic failure handling
- ✅ **Distributed** - Can run on multiple servers

## Implementation Components

### 1. Database Layer (SQL Server)

#### A. Enhanced Cleanup Stored Procedure with Audit
```sql
CREATE OR ALTER PROCEDURE sp_CleanupProcessedCallLogs_Enterprise
    @DaysToKeep INT = 30,
    @BatchSize INT = 1000,
    @MaxRunTimeMinutes INT = 60,
    @DryRun BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @StartTime DATETIME = GETDATE();
    DECLARE @EndTime DATETIME = DATEADD(MINUTE, @MaxRunTimeMinutes, @StartTime);
    DECLARE @CutoffDate DATETIME = DATEADD(DAY, -@DaysToKeep, GETDATE());
    DECLARE @AuditId UNIQUEIDENTIFIER = NEWID();
    DECLARE @TotalDeleted INT = 0;
    DECLARE @ErrorMessage NVARCHAR(4000);

    BEGIN TRY
        -- Log start of operation
        INSERT INTO CleanupAuditLog (
            AuditId, OperationType, StartTime, Parameters, Status
        ) VALUES (
            @AuditId,
            'CLEANUP_CALLLOGS',
            @StartTime,
            JSON_QUERY('{"DaysToKeep":' + CAST(@DaysToKeep AS VARCHAR) +
                      ',"BatchSize":' + CAST(@BatchSize AS VARCHAR) +
                      ',"DryRun":' + CAST(@DryRun AS VARCHAR) + '}'),
            'STARTED'
        );

        -- Create temp table for statistics
        CREATE TABLE #CleanupStats (
            TableName NVARCHAR(50),
            RecordsToDelete INT,
            RecordsDeleted INT,
            StartTime DATETIME,
            EndTime DATETIME
        );

        -- Process each table
        EXEC @TotalDeleted = sp_CleanupTable
            @TableName = 'Safaricom',
            @CutoffDate = @CutoffDate,
            @BatchSize = @BatchSize,
            @EndTime = @EndTime,
            @DryRun = @DryRun,
            @Stats = #CleanupStats;

        EXEC sp_CleanupTable
            @TableName = 'Airtel',
            @CutoffDate = @CutoffDate,
            @BatchSize = @BatchSize,
            @EndTime = @EndTime,
            @DryRun = @DryRun,
            @Stats = #CleanupStats;

        -- Update audit log
        UPDATE CleanupAuditLog
        SET
            EndTime = GETDATE(),
            Status = 'COMPLETED',
            RecordsAffected = @TotalDeleted,
            Details = (SELECT * FROM #CleanupStats FOR JSON AUTO)
        WHERE AuditId = @AuditId;

        -- Send notification if significant records deleted
        IF @TotalDeleted > 10000 AND @DryRun = 0
        BEGIN
            EXEC sp_send_dbmail
                @recipients = 'ops-team@company.com',
                @subject = 'Call Log Cleanup - Large Volume Processed',
                @body_format = 'HTML',
                @body = N'<h3>Cleanup Operation Completed</h3>
                         <p>Total Records Deleted: ' + CAST(@TotalDeleted AS NVARCHAR) + '</p>
                         <p>View details in CleanupAuditLog</p>';
        END

        DROP TABLE #CleanupStats;

    END TRY
    BEGIN CATCH
        SET @ErrorMessage = ERROR_MESSAGE();

        -- Log error
        UPDATE CleanupAuditLog
        SET
            EndTime = GETDATE(),
            Status = 'FAILED',
            ErrorMessage = @ErrorMessage
        WHERE AuditId = @AuditId;

        -- Send alert
        EXEC sp_send_dbmail
            @recipients = 'ops-team@company.com;oncall@company.com',
            @subject = 'ALERT: Call Log Cleanup Failed',
            @body = @ErrorMessage,
            @importance = 'High';

        -- Re-throw error
        THROW;
    END CATCH
END
GO
```

#### B. Create Audit Tables
```sql
CREATE TABLE CleanupAuditLog (
    AuditId UNIQUEIDENTIFIER PRIMARY KEY,
    OperationType NVARCHAR(50),
    StartTime DATETIME,
    EndTime DATETIME,
    Parameters NVARCHAR(MAX),
    Status NVARCHAR(20),
    RecordsAffected INT,
    Details NVARCHAR(MAX),
    ErrorMessage NVARCHAR(MAX),
    ExecutedBy NVARCHAR(100) DEFAULT SYSTEM_USER
);

CREATE INDEX IX_CleanupAuditLog_StartTime ON CleanupAuditLog(StartTime DESC);
```

#### C. SQL Agent Job with Advanced Settings
```sql
USE msdb;
GO

-- Create job category
IF NOT EXISTS (SELECT name FROM msdb.dbo.syscategories WHERE name=N'Data Maintenance')
BEGIN
    EXEC sp_add_category @name=N'Data Maintenance';
END
GO

-- Create the job
EXEC dbo.sp_add_job
    @job_name = N'Enterprise - Cleanup Processed Call Logs',
    @enabled = 1,
    @notify_level_eventlog = 2, -- Write to Windows Event Log on failure
    @notify_level_email = 2,    -- Email on failure
    @notify_level_page = 2,     -- Page on failure
    @notify_email_operator_name = N'DBA Team',
    @description = N'Enterprise cleanup job for processed call logs',
    @category_name = N'Data Maintenance';
GO

-- Add main cleanup step
EXEC dbo.sp_add_jobstep
    @job_name = N'Enterprise - Cleanup Processed Call Logs',
    @step_name = N'Execute Cleanup Procedure',
    @command = N'EXEC sp_CleanupProcessedCallLogs_Enterprise
                    @DaysToKeep = 30,
                    @BatchSize = 5000,
                    @MaxRunTimeMinutes = 120,
                    @DryRun = 0;',
    @database_name = N'TABDB',
    @retry_attempts = 3,
    @retry_interval = 5,
    @on_success_action = 3, -- Go to next step
    @on_fail_action = 3;    -- Go to notification step
GO

-- Add monitoring step
EXEC dbo.sp_add_jobstep
    @job_name = N'Enterprise - Cleanup Processed Call Logs',
    @step_name = N'Check Table Sizes',
    @command = N'
        -- Alert if tables are growing too large
        IF EXISTS (
            SELECT 1 FROM vw_CallLogProcessingStatus
            WHERE ProcessingStatus = 0
            AND RecordCount > 100000
        )
        BEGIN
            RAISERROR(''Warning: Over 100K unprocessed records'', 16, 1);
        END',
    @database_name = N'TABDB';
GO

-- Schedule: Daily at 2:00 AM
EXEC dbo.sp_add_schedule
    @schedule_name = N'Daily Maintenance Window',
    @freq_type = 4,      -- Daily
    @freq_interval = 1,
    @active_start_time = 020000; -- 2:00:00 AM
GO

-- Attach schedule
EXEC dbo.sp_attach_schedule
    @job_name = N'Enterprise - Cleanup Processed Call Logs',
    @schedule_name = N'Daily Maintenance Window';
GO
```

### 2. Application Layer (C# with Hangfire)

#### A. Enhanced Service with Telemetry
```csharp
public class EnterpriseCallLogCleanupService : ICallLogCleanupService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EnterpriseCallLogCleanupService> _logger;
    private readonly ITelemetryClient _telemetry;
    private readonly IEmailService _emailService;

    public async Task<CleanupResult> CleanupProcessedRecordsAsync(
        int daysToKeep = 30,
        CancellationToken cancellationToken = default)
    {
        using var operation = _telemetry.StartOperation("CallLogCleanup");
        var result = new CleanupResult { StartTime = DateTime.UtcNow };

        try
        {
            // Check if SQL Agent job is running
            var isAgentJobRunning = await CheckSqlAgentJobStatusAsync();
            if (isAgentJobRunning)
            {
                _logger.LogInformation("SQL Agent job is already running, skipping");
                result.Status = "Skipped";
                return result;
            }

            // Perform cleanup
            result = await PerformCleanupAsync(daysToKeep, cancellationToken);

            // Log metrics
            _telemetry.TrackMetric("CallLogCleanup.RecordsDeleted", result.TotalDeleted);
            _telemetry.TrackMetric("CallLogCleanup.Duration",
                (result.EndTime - result.StartTime).TotalSeconds);

            // Alert if unusual patterns
            if (result.TotalDeleted == 0 && await GetPendingRecordCountAsync() > 1000)
            {
                await _emailService.SendAlertAsync(
                    "Warning: No records deleted but many pending",
                    "Please check cleanup process configuration");
            }

            operation.Success = true;
            return result;
        }
        catch (Exception ex)
        {
            operation.Success = false;
            _logger.LogError(ex, "Cleanup failed");
            _telemetry.TrackException(ex);

            await _emailService.SendAlertAsync(
                "Critical: Call Log Cleanup Failed",
                $"Error: {ex.Message}");

            throw;
        }
    }

    [DisallowConcurrentExecution]
    public async Task MonitorCleanupHealthAsync()
    {
        // This runs every hour to check system health
        var stats = await GetCleanupStatisticsAsync();

        // Check for anomalies
        var alerts = new List<string>();

        if (stats.UnprocessedOlderThan7Days > 50000)
            alerts.Add($"High volume of old unprocessed records: {stats.UnprocessedOlderThan7Days}");

        if (stats.LastCleanupAge?.TotalHours > 36)
            alerts.Add($"Cleanup hasn't run in {stats.LastCleanupAge.Value.TotalHours:F1} hours");

        if (stats.TableSizeGB > 100)
            alerts.Add($"Database size exceeding threshold: {stats.TableSizeGB:F1} GB");

        if (alerts.Any())
        {
            await _emailService.SendAlertAsync(
                "Call Log System Health Alert",
                string.Join("\n", alerts));
        }
    }
}
```

#### B. Hangfire Configuration
```csharp
// In Program.cs
builder.Services.AddHangfire(config => config
    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.FromSeconds(15),
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true,
        SchemaName = "HangFire"
    })
    .UseFilter(new AutoRetryAttribute { Attempts = 3 })
    .UseFilter(new DisableConcurrentExecutionAttribute(timeoutInSeconds: 3600)));

// Schedule jobs
RecurringJob.AddOrUpdate<ICallLogCleanupService>(
    "monitor-cleanup-health",
    service => service.MonitorCleanupHealthAsync(),
    Cron.Hourly);

RecurringJob.AddOrUpdate<ICallLogCleanupService>(
    "backup-cleanup-job",
    service => service.CleanupProcessedRecordsAsync(30, CancellationToken.None),
    Cron.Daily(2, 30)); // 2:30 AM as backup to SQL Agent
```

### 3. Monitoring & Alerting

#### A. Application Insights Configuration
```csharp
// Custom telemetry initializer
public class CallLogTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        telemetry.Context.Component.Version = Assembly.GetExecutingAssembly()
            .GetName().Version?.ToString();

        if (telemetry is EventTelemetry eventTelemetry)
        {
            if (eventTelemetry.Name.StartsWith("CallLog"))
            {
                eventTelemetry.Properties["System"] = "CallLogManagement";
            }
        }
    }
}
```

#### B. Health Check Endpoints
```csharp
public class CallLogHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check last cleanup run
            var lastCleanup = await _context.Database
                .SqlQueryRaw<DateTime>("SELECT MAX(EndTime) FROM CleanupAuditLog")
                .FirstOrDefaultAsync(cancellationToken);

            var hoursSinceCleanup = (DateTime.UtcNow - lastCleanup).TotalHours;

            if (hoursSinceCleanup > 48)
                return HealthCheckResult.Unhealthy($"Cleanup hasn't run in {hoursSinceCleanup:F1} hours");

            if (hoursSinceCleanup > 30)
                return HealthCheckResult.Degraded($"Cleanup delayed: {hoursSinceCleanup:F1} hours");

            // Check table sizes
            var unprocessedCount = await _context.Database
                .SqlQueryRaw<int>(@"
                    SELECT COUNT(*) FROM Safaricom WHERE ProcessingStatus = 0
                    UNION ALL
                    SELECT COUNT(*) FROM Airtel WHERE ProcessingStatus = 0")
                .SumAsync(cancellationToken);

            if (unprocessedCount > 100000)
                return HealthCheckResult.Degraded($"High unprocessed count: {unprocessedCount}");

            return HealthCheckResult.Healthy($"Last cleanup: {hoursSinceCleanup:F1} hours ago");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database check failed", ex);
        }
    }
}

// Register in Program.cs
builder.Services.AddHealthChecks()
    .AddCheck<CallLogHealthCheck>("calllogs", tags: new[] { "database", "critical" });
```

### 4. Disaster Recovery

#### Backup Before Major Cleanup
```sql
CREATE PROCEDURE sp_BackupBeforeCleanup
AS
BEGIN
    DECLARE @BackupFile NVARCHAR(500);
    SET @BackupFile = 'C:\Backups\TABDB_PreCleanup_' +
                      CONVERT(NVARCHAR, GETDATE(), 112) + '.bak';

    BACKUP DATABASE TABDB
    TO DISK = @BackupFile
    WITH COMPRESSION, CHECKSUM, STATS = 10;

    -- Verify backup
    RESTORE VERIFYONLY FROM DISK = @BackupFile;
END
```

## Deployment Checklist

- [ ] SQL Agent service is running and configured
- [ ] Email operators configured in SQL Server
- [ ] Hangfire dashboard secured with authentication
- [ ] Application Insights configured
- [ ] Health check endpoints monitored
- [ ] Backup strategy in place
- [ ] Runbook documented for operations team
- [ ] Alerts configured in monitoring system
- [ ] Performance baselines established
- [ ] Disaster recovery plan tested

## Key Metrics to Monitor

| Metric | Warning Threshold | Critical Threshold |
|--------|------------------|-------------------|
| Hours since last cleanup | > 30 | > 48 |
| Unprocessed record count | > 50,000 | > 100,000 |
| Cleanup duration | > 60 min | > 120 min |
| Failed cleanup attempts | > 1/week | > 2/day |
| Database size | > 80 GB | > 100 GB |
| Deleted records per run | < 100 | 0 |

## Operations Runbook

### If Cleanup Fails:
1. Check SQL Agent job history
2. Review CleanupAuditLog table
3. Check Application Insights for exceptions
4. Verify database connectivity
5. Check disk space
6. Run manual cleanup with smaller batch size
7. Contact DBA team if persistent

### Monthly Maintenance:
1. Review cleanup statistics
2. Adjust retention policies if needed
3. Analyze growth trends
4. Update capacity planning
5. Test disaster recovery