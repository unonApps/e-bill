# Call Log Cleanup Implementation Guide

## Overview
This guide explains how to implement automatic cleanup of processed call logs.

## Method 1: Using Hangfire (Recommended for .NET Applications)

### 1. Install Hangfire NuGet Package
```bash
dotnet add package Hangfire.AspNetCore
dotnet add package Hangfire.SqlServer
```

### 2. Configure in Program.cs
```csharp
// Add Hangfire services
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString));

builder.Services.AddHangfireServer();

// Register cleanup service
builder.Services.AddScoped<ICallLogCleanupService, CallLogCleanupService>();

// After app.Build()
app.UseHangfireDashboard("/hangfire");

// Schedule the cleanup job to run daily at 2 AM
RecurringJob.AddOrUpdate<ICallLogCleanupService>(
    "cleanup-processed-logs",
    service => service.CleanupProcessedRecordsAsync(30),
    Cron.Daily(2, 0)); // 2:00 AM daily
```

### 3. Manual Trigger from Admin Page
```csharp
// In CallLogStaging.cshtml.cs
public async Task<IActionResult> OnPostCleanupAsync()
{
    var deleted = await _cleanupService.CleanupProcessedRecordsAsync(30);
    TempData["Success"] = $"Cleaned up {deleted} processed records";
    return RedirectToPage();
}
```

## Method 2: Using SQL Server Agent (Database Level)

### 1. Create SQL Agent Job
```sql
USE msdb;
GO

-- Create job
EXEC dbo.sp_add_job
    @job_name = N'Cleanup Processed Call Logs',
    @enabled = 1,
    @description = N'Deletes processed call logs older than 30 days';
GO

-- Add job step
EXEC dbo.sp_add_jobstep
    @job_name = N'Cleanup Processed Call Logs',
    @step_name = N'Execute Cleanup',
    @command = N'EXEC sp_CleanupProcessedCallLogs @DaysToKeep=30, @TestMode=0;',
    @database_name = N'TABDB';
GO

-- Schedule to run daily at 2 AM
EXEC dbo.sp_add_schedule
    @schedule_name = N'Daily at 2 AM',
    @freq_type = 4,
    @freq_interval = 1,
    @active_start_time = 020000; -- 2:00 AM
GO

-- Attach schedule to job
EXEC dbo.sp_attach_schedule
    @job_name = N'Cleanup Processed Call Logs',
    @schedule_name = N'Daily at 2 AM';
GO
```

## Method 3: Windows Task Scheduler + PowerShell

### 1. Create PowerShell Script
```powershell
# CleanupCallLogs.ps1
$connectionString = "Server=MICHUKI\SQLEXPRESS;Database=TABDB;Integrated Security=true"
$query = "EXEC sp_CleanupProcessedCallLogs @DaysToKeep=30, @TestMode=0;"

Invoke-Sqlcmd -ConnectionString $connectionString -Query $query

# Log the result
$logFile = "C:\Logs\CallLogCleanup_$(Get-Date -Format 'yyyyMMdd').log"
"Cleanup executed at $(Get-Date)" | Out-File $logFile -Append
```

### 2. Schedule in Task Scheduler
- Open Task Scheduler
- Create Basic Task
- Trigger: Daily at 2:00 AM
- Action: Start PowerShell.exe with script path

## Method 4: Azure Functions (For Cloud Deployment)

### Timer-Triggered Function
```csharp
[FunctionName("CleanupCallLogs")]
public static async Task Run(
    [TimerTrigger("0 0 2 * * *")] TimerInfo timer, // 2 AM daily
    ILogger log)
{
    var connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

    using (var connection = new SqlConnection(connectionString))
    {
        await connection.OpenAsync();
        using (var command = new SqlCommand("sp_CleanupProcessedCallLogs", connection))
        {
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddWithValue("@DaysToKeep", 30);
            command.Parameters.AddWithValue("@TestMode", 0);

            var result = await command.ExecuteNonQueryAsync();
            log.LogInformation($"Cleanup completed, affected rows: {result}");
        }
    }
}
```

## Monitoring and Alerts

### Check Cleanup Status
```sql
-- View current status
SELECT * FROM vw_CallLogProcessingStatus;

-- Check last cleanup run (if logging enabled)
SELECT TOP 10 * FROM CleanupLog ORDER BY ExecutionDate DESC;

-- Alert if too many unprocessed records
IF EXISTS (
    SELECT 1 FROM vw_CallLogProcessingStatus
    WHERE ProcessingStatus = 0 AND RecordCount > 10000
)
BEGIN
    -- Send email alert or log warning
    EXEC sp_send_dbmail @recipients = 'admin@company.com',
        @subject = 'High volume of unprocessed call logs',
        @body = 'Please review call log processing';
END
```

## Testing the Cleanup

### 1. Test Mode (See what would be deleted)
```sql
EXEC sp_CleanupProcessedCallLogs @DaysToKeep=30, @TestMode=1;
```

### 2. Manual Execution
```csharp
// From C# code
await _cleanupService.CleanupProcessedRecordsAsync(30);
```

### 3. Verify Results
```sql
-- Check record counts before and after
SELECT
    TableName,
    StatusName,
    RecordCount,
    OldestRecord
FROM vw_CallLogProcessingStatus
ORDER BY TableName, ProcessingStatus;
```

## Retention Policy

| Data Type | Retention Period | Cleanup Method |
|-----------|-----------------|----------------|
| Source Tables (New) | Until Processed | Never deleted automatically |
| Source Tables (Processed) | 30 days | Daily cleanup job |
| Staging Table | 90 days | Daily cleanup job |
| Production CallRecords | 7 years | Annual archive process |

## Best Practices

1. **Always test first** using TestMode=1
2. **Monitor disk space** regularly
3. **Keep cleanup logs** for audit
4. **Set up alerts** for failures
5. **Backup before major cleanups**
6. **Run during off-peak hours** (2-4 AM)