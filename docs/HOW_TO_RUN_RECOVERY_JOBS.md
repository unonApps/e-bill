# How to Run Recovery Jobs - Complete Guide

## Overview
The Recovery Job System automatically processes expired deadlines and recovers call records according to defined rules. Jobs can run **automatically** (on schedule) or **manually** (triggered by admin).

---

## 🎯 How to Run Recovery Jobs MANUALLY

### Step 1: Navigate to Recovery Job Status Page
1. Go to: **http://localhost:5041/Admin/RecoveryJobStatus**
2. Or navigate: **Admin Menu → Recovery Dashboard → Job Status**

### Step 2: Check Current Status
Before triggering a job, verify:
- ✅ **No job is currently running** (Status shows "Idle")
- ✅ Last execution completed successfully
- ✅ No errors displayed

### Step 3: Trigger Manual Run
1. Click the **"Trigger Manual Run"** button (green button in top-right)
2. Confirm the action when prompted
3. Wait for success message: "Recovery job has been triggered manually"

### Step 4: Monitor Progress
- Page auto-refreshes every 30 seconds when a job is running
- Watch the "Current Status" card change from "Idle" to "Running"
- Check the execution history table for real-time updates

### Step 5: View Results
Once completed:
- **Status**: Shows "Completed" with green badge
- **Records**: Number of call records processed
- **Amount**: Total dollar amount recovered
- **Duration**: How long the job took

---

## 📊 What the Recovery Job Does

When you trigger a recovery job (manual or automatic), it runs these steps in order:

### **Step 1: Send Deadline Reminders**
- Sends email reminders to staff with approaching deadlines
- Sends reminders to supervisors with pending approvals

### **Step 2: Process Expired Staff Verifications**
- **Rule 1:** Unverified calls → Recovered as PERSONAL
- Finds calls past verification deadline that staff never verified

### **Step 2A: Process Verified But Not Submitted** 🆕 **NEW**
- **Rule 1A:** Verified as Personal → Recovered as PERSONAL
- **Rule 1B:** Verified as Official but NOT submitted → Recovered as PERSONAL
- Ensures official calls MUST be submitted for supervisor approval

### **Step 3: Process Expired Supervisor Approvals**
- **Rule 2:** Verified + Submitted but supervisor didn't act → Recovered as CLASS OF SERVICE
- Finds calls submitted to supervisor but approval deadline passed

### **Step 4: Process Reverted Verifications**
- **Rule 4:** Supervisor reverted but staff didn't resubmit → Recovered as PERSONAL
- Finds calls that supervisors sent back for correction

---

## 🔄 Automatic Recovery (Background Job)

The recovery job also runs **automatically** on a schedule.

### Check Automation Status
1. Go to: **Admin → Recovery Dashboard → Settings**
2. Look for "Automation Settings" section
3. Check:
   - **Automation Enabled**: Should be ON (green toggle)
   - **Job Interval**: How often it runs (default: every 1 hour)

### Default Schedule
- Runs every **60 minutes** (configurable)
- Checks for expired deadlines
- Processes all recovery rules automatically
- No manual intervention needed

### Enable/Disable Automation
1. Go to Recovery Settings page
2. Toggle "Automation Enabled" switch
3. Click "Save Configuration"

---

## 🚫 Troubleshooting - Why Can't I Run Recovery?

### Issue 1: "A recovery job is already running"
**Cause:** Another job is currently executing
**Solution:**
- Wait for current job to complete (check status)
- If job is stuck (running >2 hours), it will auto-fail
- Or manually cancel the running job:
  1. Find the running job in execution history
  2. Click "Cancel" button
  3. Confirm cancellation

### Issue 2: Button doesn't work / No response
**Cause:** Page needs refresh or browser cache issue
**Solution:**
1. Refresh the page (F5)
2. Clear browser cache
3. Check browser console for errors (F12)

### Issue 3: Job completes but no records processed
**Possible Causes:**
1. **No expired deadlines exist**
   - Check Recovery Dashboard for upcoming deadlines
   - Verify batches have verification/approval deadlines set

2. **All calls already recovered**
   - Check Recovery Logs to see past recoveries
   - View batch details to confirm call statuses

3. **Deadlines not yet passed**
   - Recovery only processes EXPIRED deadlines
   - Check deadline dates in Recovery Dashboard

### Issue 4: Job fails with error
**Steps to diagnose:**
1. Click "View Log" button on failed execution
2. Read error message and execution log
3. Check for:
   - Database connection issues
   - Missing batch data
   - Service configuration errors

---

## 📈 Understanding Recovery Job Results

### Execution Details Modal
Click the **eye icon** on any execution to see:

#### Execution Info
- **Execution ID**: Unique identifier for this run
- **Status**: Completed / Running / Failed
- **Run Type**: Manual / Automatic
- **Triggered By**: Username or "System"

#### Timing
- **Start Time**: When job began
- **End Time**: When job finished
- **Duration**: Total execution time
- **Next Run**: When next automatic run scheduled

#### Processing Summary
- **Expired Verifications**: Unverified calls processed
- **Expired Approvals**: Unapproved calls processed
- **Reverted Verifications**: Reverted calls processed
- **Total Records**: All calls recovered
- **Total Amount**: Total $ recovered
- **Reminders Sent**: Email reminders sent

#### Execution Log
Detailed line-by-line log of what happened:
```
[2025-10-28 14:30:15] Job started
Run Type: Manual

--- Step 1: Deadline Reminders ---
Deadline reminders sent successfully

--- Step 2: Expired Verification Deadlines ---
Found 2 expired verification deadlines

Processing Batch: a1b2c3d4...
  ✓ SUCCESS: 5 records, $450.00 recovered
  Message: Processed 5 unverified calls...

--- Step 2A: Verified But Not Submitted Calls ---
Checking 3 active batches
✓ Processed 2 verified but not submitted calls...

--- Step 3: Expired Approval Deadlines ---
Found 1 expired approval deadline...

--- Step 4: Reverted Verifications ---
Checking 3 active batches for reverted verifications

--- Job Completed ---
Duration: 1234ms
Total Records: 8
Total Amount: $1,200.00
```

---

## 🔍 Checking What Will Be Recovered

### Before Running Recovery
To see what WILL be recovered, check:

#### 1. Recovery Dashboard
- Go to: **Admin → Recovery Dashboard**
- View "Upcoming Deadlines" section
- See all batches with deadlines approaching/expired

#### 2. Call Records
Query the database to see eligible calls:
```sql
-- Calls that WILL be recovered (unverified + expired)
SELECT
    Id,
    ext_no,
    call_date,
    call_cost_usd,
    verification_period as 'Deadline',
    call_ver_ind as 'Verified',
    'Will recover as PERSONAL' as 'Action'
FROM CallRecords
WHERE call_ver_ind = 0
  AND verification_period < GETDATE()
  AND recovery_status = 'NotProcessed';

-- Calls verified but not submitted
SELECT
    cr.Id,
    cr.ext_no,
    cr.call_date,
    cr.call_cost_usd,
    cr.verification_type,
    cr.verification_period,
    'Will recover as PERSONAL (not submitted)' as 'Action'
FROM CallRecords cr
WHERE cr.call_ver_ind = 1
  AND cr.verification_period < GETDATE()
  AND cr.recovery_status = 'NotProcessed'
  AND NOT EXISTS (
      SELECT 1 FROM CallLogVerifications clv
      WHERE clv.CallRecordId = cr.Id
      AND clv.SubmittedToSupervisor = 1
  );
```

---

## 📝 Best Practices

### When to Run Manual Recovery
1. **After importing a new batch** - To process any immediate expirations
2. **End of approval period** - To finalize all pending approvals
3. **Before generating reports** - To ensure all recoveries are current
4. **After system maintenance** - To catch up on missed automatic runs

### What NOT to Do
❌ Don't run recovery multiple times in quick succession
❌ Don't cancel jobs unless they're genuinely stuck
❌ Don't modify database records during recovery execution
❌ Don't disable automation without a good reason

### Recommended Schedule
- **Automatic**: Every 1-2 hours (production)
- **Manual**: Only when needed (imports, reports, troubleshooting)
- **Monitor**: Check job status daily for failures

---

## 🎛️ Configuration Options

### Recovery Settings Page
Navigate to: **Admin → Recovery Dashboard → Settings**

#### Automation Settings
- **Enable/Disable Automation**: Turn background job on/off
- **Job Interval**: How often to run (minutes)
- **Timeout Threshold**: Max execution time before marked as failed

#### Recovery Rules
View and configure all recovery rules:
1. Staff Non-Verification → Personal
2. Verified as Personal → Personal 🆕
3. Official Not Submitted → Personal 🆕
4. Supervisor Non-Approval → Class of Service
5. Partial Approval → Split recovery
6. Supervisor Revert → Personal
7. Manual Override → As specified

---

## 📧 Email Notifications

### Who Gets Notified
After recovery runs, emails are sent to:
1. **Staff**: Notified when their calls are recovered
2. **Supervisors**: Summary of recovered calls from their team
3. **Admin**: Daily summary of all recovery activity

### Email Content
Staff notifications include:
- Number of calls recovered
- Total amount recovered
- Reason for recovery (which rule triggered)
- Action to take (if any)

---

## 📊 Monitoring and Reports

### View Recovery History
1. **Recovery Job Status**: See all past executions
2. **Recovery Logs**: Detailed logs for each call
3. **Recovery Reports**: Summary reports by batch/staff/period

### Key Metrics to Monitor
- **Success Rate**: Should be >95%
- **Average Duration**: Should be <2 minutes
- **Total Amount Recovered**: Track trends over time
- **Failed Executions**: Should be 0 or minimal

---

## 🆘 Getting Help

### If You Need Assistance
1. **Check Execution Log**: Click eye icon on failed job
2. **Review Error Message**: Read what went wrong
3. **Check Database**: Verify data integrity
4. **Contact System Admin**: Provide execution ID and error details

### Common Error Messages

#### "Batch not found"
- Batch was deleted or ID is invalid
- Check batch exists in StagingBatches table

#### "Service not registered"
- Dependency injection issue
- Restart application to reload services

#### "Database connection failed"
- Connection string issue
- Check appsettings.json database configuration

#### "Timeout - exceeded 2 hour limit"
- Job took too long (usually data issue)
- Check for large batches or slow queries

---

## 🎉 Success! You're Ready

You now know how to:
✅ Trigger manual recovery jobs
✅ Monitor job execution status
✅ View detailed execution logs
✅ Understand recovery rules
✅ Troubleshoot common issues
✅ Configure automation settings

**Quick Start:** Go to `http://localhost:5041/Admin/RecoveryJobStatus` and click "Trigger Manual Run"!
