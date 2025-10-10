# Azure Database Migration Fix Instructions

Your Azure database is missing tables and columns. Follow these steps **IN ORDER** to fix the schema mismatch.

## ⚠️ Important
Run these scripts in **Azure Portal Query Editor** in the exact order below.

---

## Step 1: Sync Migration History

**File:** `sync-azure-migrations.sql`

**Purpose:** Updates the `__EFMigrationsHistory` table so EF Core knows which migrations have been applied.

**Status:** ✅ Already completed (based on earlier execution)

---

## Step 2: Create Missing Tables

**File:** `add-missing-call-log-tables.sql`

**Purpose:** Creates the 3 missing call log verification tables:
- `CallLogVerifications`
- `CallLogPaymentAssignments`
- `CallLogDocuments`

**Instructions:**
1. Open Azure Portal → Your SQL Database (`tabdb`)
2. Click **Query editor**
3. Copy entire content from `add-missing-call-log-tables.sql`
4. Paste and click **Run**
5. Verify output shows "Missing Tables Created Successfully!"

---

## Step 3: Add Missing Columns

**File:** `complete-azure-schema-fix.sql`

**Purpose:** Adds ALL missing columns to existing tables:

- **CallRecords:** UserPhoneId, verification_type, payment_assignment_id, assignment_status, overage_justified, supervisor_approval_status, supervisor_approved_by, supervisor_approved_date
- **ClassOfServices:** AirtimeAllowanceAmount, DataAllowanceAmount, BillingPeriod, HandsetAllowanceAmount
- **UserPhones:** Status
- **EbillUsers:** ApplicationUserId, HasLoginAccount, LoginEnabled
- **CallLogVerifications:** OverageAmount, OverageJustified, PaymentAssignmentId, SupervisorApprovedBy, + column renames
- **Notifications:** Creates entire table if missing

**Instructions:**
1. In the same Query editor
2. Copy entire content from `complete-azure-schema-fix.sql`
3. Paste and click **Run**
4. Verify output shows column counts at the end:
   - CallRecords: 9 columns
   - ClassOfServices: 4 columns
   - EbillUsers: 3 columns

---

## Step 4: Restart Azure App Service

After both scripts complete successfully:

1. Go to **Azure Portal** → Your App Service
2. Click **Restart**
3. Wait for the application to fully restart
4. Test the application

---

## Troubleshooting

### If you get "Invalid column name" errors:
- The column wasn't added properly. Re-run `complete-azure-schema-fix.sql`
- Check the script output for any errors

### If you get "Invalid object name" errors:
- A table is missing. Re-run `add-missing-call-log-tables.sql`

### If migrations keep trying to recreate tables:
- The `__EFMigrationsHistory` is out of sync
- Run `sync-azure-migrations.sql` again
- Make sure ALL 28 migrations are recorded

---

## What Caused This Issue?

The migration `20251002163017_AddCallLogVerificationSystemTables` was generated incorrectly - it tried to create ALL database tables instead of just the 3 call log verification tables. This caused:

1. Migration history to get out of sync with actual schema
2. Multiple tables to be missing in Azure
3. Many columns from recent migrations to not be applied

These scripts fix the schema without regenerating the bad migration.

---

## Current Migrations (28 total)

All 28 migrations should be in `__EFMigrationsHistory` after Step 1:
1. 20250620192830_InitialCreate
2. 20250620200642_AddUserStatus
3. ... (26 more)
28. 20251008095859_AddNotificationsTable

---

## Need Help?

If errors persist after following all steps:
1. Check the script output for specific error messages
2. Verify your database credentials are correct
3. Make sure you're connected to the correct Azure SQL database (`tabdb`)
