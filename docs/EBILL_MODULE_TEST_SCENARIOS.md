# E-Bill Module Test Scenarios

This document provides comprehensive test scenarios for the E-Bill Management module, covering all major workflows from user creation through the recovery process.

---

## Table of Contents

1. [E-Bill User Management](#1-e-bill-user-management)
2. [Call Log Data Upload](#2-call-log-data-upload)
3. [Billing Processing (Simplified Workflow)](#3-billing-processing-simplified-workflow)
4. [Consolidation Batch Creation (Manual)](#4-consolidation-batch-creation-manual)
5. [Batch Verification (Admin)](#5-batch-verification)
6. [Push to Production](#6-push-to-production)
7. [Staff Call Verification (User Self-Service)](#7-staff-call-verification-user-self-service)
8. [Supervisor Approval Workflow](#8-supervisor-approval-workflow)
9. [Recovery Process (EOS)](#9-recovery-process-eos---end-of-service)
10. [End-to-End Integration Tests](#10-end-to-end-integration-tests)

---

## 1. E-Bill User Management

### 1.1 Create Single E-Bill User

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| EU-001 | Create new E-Bill user with valid data | 1. Navigate to Admin > E-Bill Users<br>2. Click "Add User"<br>3. Fill all required fields (FirstName, LastName, IndexNumber, Email, MobileNumber)<br>4. Click Save | User created successfully. UserPhone record automatically created with mobile number as primary. Success message displayed. |
| EU-002 | Create user with duplicate Index Number | 1. Create user with Index "12345"<br>2. Attempt to create another user with same Index "12345" | Error: "Index Number already exists" |
| EU-003 | Create user with duplicate Email | 1. Create user with email "test@un.org"<br>2. Attempt to create another user with same email | Error: "Email already exists" |
| EU-004 | Create user with duplicate Phone Number | 1. Create user with phone "+254712345678"<br>2. Attempt to create another user with same phone | Error: "Phone number already assigned to another user" |
| EU-005 | Create user with login account | 1. Fill user details<br>2. Check "Create Login Account"<br>3. Enter password<br>4. Save | User created with ApplicationUser record. Email sent with credentials. User can login. |
| EU-006 | Create user without login account | 1. Fill user details<br>2. Leave "Create Login Account" unchecked<br>3. Save | User created. No ApplicationUser record. No email sent. |
| EU-007 | Create user with invalid email format | 1. Enter email without @ symbol<br>2. Save | Validation error: "Invalid email format" |
| EU-008 | Create user with invalid phone format | 1. Enter phone with letters "ABC123"<br>2. Save | Validation error: "Invalid phone format" |

### 1.2 Edit E-Bill User

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| EU-009 | Update user basic info | 1. Select existing user<br>2. Modify FirstName, LastName<br>3. Save | Changes saved. Audit log entry created. |
| EU-010 | Update user phone number | 1. Select user<br>2. Change MobileNumber<br>3. Save | UserPhone record updated. Old number marked inactive. |
| EU-011 | Deactivate user | 1. Select active user<br>2. Set IsActive = false<br>3. Save | User deactivated. User cannot login (if has account). |
| EU-012 | Reactivate user | 1. Select inactive user<br>2. Set IsActive = true<br>3. Save | User reactivated. Access restored. |

### 1.3 Bulk Import E-Bill Users

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| EU-013 | Import valid CSV file | 1. Download CSV template<br>2. Fill with valid data (10 users)<br>3. Upload CSV<br>4. Submit | All 10 users imported successfully. Summary shows 10 created, 0 errors. |
| EU-014 | Import with duplicate records | 1. Upload CSV with existing IndexNumbers<br>2. Select "Skip Duplicates"<br>3. Submit | Existing users skipped. Only new users created. Summary shows counts. |
| EU-015 | Import with update mode | 1. Upload CSV with existing IndexNumbers<br>2. Select "Update Existing"<br>3. Submit | Existing users updated with new data. New users created. |
| EU-016 | Import with invalid CSV structure | 1. Upload CSV with missing required columns<br>2. Submit | Error: "Missing required columns: [list]" |
| EU-017 | Import empty CSV | 1. Upload CSV with only headers<br>2. Submit | Error: "No data rows found in CSV" |
| EU-018 | Import with invalid data rows | 1. Upload CSV with invalid emails/phones<br>2. Submit | Partial success. Summary shows valid imports and error rows with line numbers. |

---

## 2. Call Log Data Upload

### 2.1 Safaricom Data Upload

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| CL-001 | Upload valid Safaricom CSV | 1. Navigate to Admin > Call Logs Upload<br>2. Select "Safaricom"<br>3. Select billing month/year<br>4. Upload valid CSV<br>5. Submit | ImportJob created with status "Queued". Background job starts. Progress updates visible. |
| CL-002 | Monitor Safaricom import progress | 1. Upload large file (100K+ records)<br>2. Navigate to Import Jobs<br>3. Observe progress | Progress percentage updates. Records processed count increases. |
| CL-003 | Complete Safaricom import | 1. Wait for import to complete<br>2. Check Import Jobs page | Status = "Completed". RecordsSuccess shows count. Records in Safaricom table. |
| CL-004 | Upload with wrong date format | 1. Upload CSV with dates in wrong format<br>2. Submit | Import completes with warnings. Rows with invalid dates logged as errors. |
| CL-005 | Upload with missing phone lookup | 1. Upload CSV with phone numbers not in UserPhones<br>2. Submit | Records imported with NULL IndexNumber/EbillUserId. Warning logged. |

### 2.2 Airtel Data Upload

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| CL-006 | Upload valid Airtel CSV | 1. Select "Airtel"<br>2. Upload valid Airtel format CSV<br>3. Submit | ImportJob created. Records inserted into Airtel table. |
| CL-007 | Upload Airtel with enterprise batch size | 1. Upload file with 500K records<br>2. Monitor progress | Processes in 50K batches. Memory efficient. Completes successfully. |

### 2.3 PSTN Data Upload

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| CL-008 | Upload valid PSTN CSV | 1. Select "PSTN"<br>2. Upload valid PSTN format CSV<br>3. Submit | Records inserted into PSTN table with proper transformations. |
| CL-009 | PSTN with duration calculations | 1. Upload PSTN with raw duration data<br>2. Verify calculations | Duration properly calculated. Cost calculations correct. |

### 2.4 Private Wire Data Upload

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| CL-010 | Upload valid PrivateWire CSV | 1. Select "PrivateWire"<br>2. Upload valid format<br>3. Submit | Records inserted into PrivateWires table. |

### 2.5 Import Job Management

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| CL-011 | View all import jobs | 1. Navigate to Admin > Import Jobs<br>2. Apply no filters | All jobs displayed with status, progress, timestamps. |
| CL-012 | Filter jobs by status | 1. Select "Running" filter<br>2. Apply | Only running jobs displayed. |
| CL-013 | Filter jobs by date range | 1. Set date range<br>2. Apply | Jobs within range displayed. |
| CL-014 | Cancel running job | 1. Find running job<br>2. Click Cancel | Job status changes to "Cancelled". Background job terminated. |
| CL-015 | View job error details | 1. Find failed job<br>2. Click to view details | Error message displayed. Row-level errors listed. |

---

## 3. Billing Processing (Simplified Workflow)

> **Page:** `/Admin/BillingProcessing`
>
> This is the **recommended** streamlined approach that combines consolidation, anomaly detection, auto-verification, and push to production into a single automated workflow.

### 3.1 Preview and Configuration

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| BP-001 | Preview billing records | 1. Navigate to Admin > Billing Processing<br>2. Select billing month/year<br>3. Set verification period (days)<br>4. Click "Preview Records" | Preview card shows: Safaricom, Airtel, PSTN, PrivateWire counts. Total records displayed. Staff and Supervisor deadlines calculated. |
| BP-002 | Preview with no records | 1. Select month with no uploaded data<br>2. Click Preview | Total count = 0. Start Processing button disabled with message "No Records Found". |
| BP-003 | Preview without exchange rate | 1. Select month without exchange rate configured<br>2. Click Preview | Warning: "No exchange rate for [Month]". Add button displayed. Start Processing button disabled. |
| BP-004 | Add exchange rate from preview | 1. Preview shows missing exchange rate<br>2. Click "Add" button<br>3. Enter rate (e.g., 150.5000)<br>4. Save | Exchange rate saved. Preview refreshes. Green checkmark: "Exchange Rate: 1 USD = X KES". |
| BP-005 | Verify deadline calculations | 1. Set verification period to 7 days<br>2. Click Preview | Staff Deadline = Today + 7 days. Supervisor Deadline = Today + 10 days (7+3). |

### 3.2 Start Processing

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| BP-006 | Start billing processing | 1. Preview valid records<br>2. Click "Start Processing"<br>3. Review confirmation modal<br>4. Select "Official" classification<br>5. Click "Yes, Start Processing" | Batch created. Progress card appears. Steps show: Consolidating → Detecting anomalies → Auto-verifying → Pushing to production. |
| BP-007 | Start with Personal classification | 1. Start processing<br>2. Select "Personal" classification<br>3. Confirm | All records pushed with VerificationType = "Personal". |
| BP-008 | Block duplicate processing | 1. Start processing for January 2025<br>2. While running, try to start another for January 2025 | Error: "A batch is currently being processed. Please wait for it to complete." |
| BP-009 | Multiple runs same month | 1. Complete processing for January 2025<br>2. Start another run for January 2025 | New batch created: "Billing January 2025 (Run 2)". Previous batch remains intact. |

### 3.3 Progress Monitoring

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| BP-010 | Monitor real-time progress | 1. Start processing<br>2. Watch progress card | Progress bar updates. Percentage increases. Current operation text updates through steps. |
| BP-011 | Step indicators update | 1. Start processing<br>2. Watch step indicators | Steps highlight as active (blue), then complete (green checkmark). |
| BP-012 | Resume monitoring on page reload | 1. Start processing<br>2. Refresh page while processing | Current processing alert shows. Progress polling resumes automatically. |

### 3.4 Processing Completion

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| BP-013 | Successful completion | 1. Wait for processing to complete | Results panel shows: Total Records, Published count, Anomalies count. Success message displayed. |
| BP-014 | Completion with anomalies | 1. Process batch with anomaly records<br>2. Complete processing | Anomaly count > 0. "Download Anomaly Report" button appears. |
| BP-015 | Download anomaly report | 1. After completion with anomalies<br>2. Click "Download Anomaly Report" | CSV downloaded with columns: Extension, Call Date, Dialed Number, Duration, Cost, Call Type, Anomaly Reason. |
| BP-016 | Processing failure handling | 1. Processing fails (e.g., database error)<br>2. Check UI | Progress card shows failure. Batch status = "Failed". Error details available in history. |

### 3.5 Processing History

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| BP-017 | View processing history | 1. Navigate to Billing Processing<br>2. View history table | Last 12 batches displayed with: Batch name, Status, Records, Published, Anomalies, Date, Actions. |
| BP-018 | Send notifications for published batch | 1. Find published batch in history<br>2. Click envelope icon<br>3. Confirm | Notification job queued. Success message. Button shows checkmark temporarily. |
| BP-019 | View failure details | 1. Find failed batch in history<br>2. Click error icon | Modal shows: Batch name, Error details, Stack trace, Date, Created by. |
| BP-020 | Download anomalies from history | 1. Find batch with anomalies > 0<br>2. Click download icon | CSV file downloaded with anomaly records for that batch. |

### 3.6 Email Queue Monitoring

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| BP-021 | View email queue statistics | 1. Navigate to Billing Processing<br>2. View Email Queue section | Shows: Total Emails, Queued, Sent, Failed counts. |
| BP-022 | Email processing info | 1. Emails are queued<br>2. View Email Queue section | Info message: "X emails are queued and will be processed automatically (50 emails every 2 minutes)". |

### 3.7 Anomaly Types Detected

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| BP-023 | Long duration anomaly | 1. Upload call with duration > 60 minutes<br>2. Process billing | Record flagged with "Long Duration (>60 min)" anomaly. |
| BP-024 | Duplicate record anomaly | 1. Upload duplicate call records<br>2. Process billing | Duplicates flagged with "Duplicate Record" anomaly. |
| BP-025 | Unregistered extension | 1. Upload call with unknown extension<br>2. Process billing | Record flagged with "Extension Not Registered" anomaly. |
| BP-026 | Zero cost anomaly | 1. Upload call with cost = 0<br>2. Process billing | Record flagged with "Zero Cost" anomaly. |
| BP-027 | Future date anomaly | 1. Upload call with future date<br>2. Process billing | Record flagged with "Future Date" anomaly. |
| BP-028 | Unassigned phone anomaly | 1. Upload call for phone not assigned to any user<br>2. Process billing | Record flagged with "Unassigned Phone Number" anomaly. |

---

## 4. Consolidation Batch Creation (Manual)

> **Page:** `/Admin/CallLogStaging`
>
> This is the **manual/advanced** approach where each step (consolidation, verification, push) is performed separately. Use this for more granular control.

### 4.1 Standard Monthly Consolidation

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| CB-001 | Create consolidation batch | 1. Navigate to Admin > Call Log Staging<br>2. Select billing month<br>3. Click "Consolidate" | StagingBatch created with status "Created". Background job queued. |
| CB-002 | Monitor consolidation progress | 1. Create batch<br>2. Observe progress | Status changes: Created → Processing. Record counts update. |
| CB-003 | Complete consolidation | 1. Wait for completion<br>2. Refresh page | Status = "Processing" complete. CallLogStaging records created. Statistics populated. |
| CB-004 | Consolidate with no source data | 1. Select month with no call logs<br>2. Click Consolidate | Error: "No records found for selected period" |
| CB-005 | Consolidate duplicate month | 1. Create batch for January 2025<br>2. Try to create another batch for January 2025 | Error: "A batch already exists for this period" |
| CB-006 | Consolidate all sources | 1. Upload data to all 4 sources (Safaricom, Airtel, PSTN, PrivateWire)<br>2. Create consolidation batch | All source records consolidated into CallLogStaging. Source breakdown visible. |

### 4.2 Interim Consolidation (EOS/Separation)

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| CB-007 | Create interim batch for separating staff | 1. Click "Consolidate Interim"<br>2. Enter staff Index Number<br>3. Enter separation date/reason<br>4. Select billing month<br>5. Submit | INTERIM batch created. Only selected staff's records included. |
| CB-008 | Interim with no records for staff | 1. Enter Index with no call records<br>2. Submit | Error: "No call records found for this staff member" |
| CB-009 | Interim with multiple phone numbers | 1. Staff has 3 phone numbers<br>2. Create interim batch | All 3 phones' records consolidated into batch. |

### 4.3 Batch Statistics and Management

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| CB-010 | View batch statistics | 1. Select existing batch<br>2. View statistics panel | Shows: Total records, Pending, Verified, Rejected, With anomalies counts. |
| CB-011 | View records by batch | 1. Select batch from dropdown<br>2. Apply filter | Only records for selected batch displayed. |
| CB-012 | Search within batch | 1. Select batch<br>2. Enter search term (phone/name)<br>3. Search | Filtered results within batch displayed. |
| CB-013 | Delete failed batch | 1. Find batch with status "Failed"<br>2. Click Delete | Batch and related staging records deleted. Source records reset to "Staged". |

---

## 5. Batch Verification

### 5.1 Record-Level Verification

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| VR-001 | Verify single record | 1. Select pending record<br>2. Click Verify | VerificationStatus = "Verified". VerificationDate set. VerifiedBy = current user. |
| VR-002 | Reject single record | 1. Select pending record<br>2. Enter rejection reason<br>3. Click Reject | VerificationStatus = "Rejected". Reason saved. |
| VR-003 | Verify record with anomalies | 1. Find record with HasAnomalies = true<br>2. Review anomalies<br>3. Verify anyway | Record verified. Anomaly flag remains for reporting. |
| VR-004 | View anomaly details | 1. Click anomaly indicator<br>2. View details | Modal shows: Anomaly type, severity, description, threshold values. |

### 5.2 Bulk Verification

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| VR-005 | Verify all pending in batch | 1. Select batch<br>2. Click "Verify All"<br>3. Confirm | Background job queued. All pending records verified. |
| VR-006 | Reject multiple selected | 1. Select multiple records via checkbox<br>2. Enter reason<br>3. Click "Reject Selected" | All selected records rejected with same reason. |
| VR-007 | Verify All with RequiresReview records | 1. Batch has RequiresReview records<br>2. Click Verify All | Only Pending records verified. RequiresReview records unchanged. |

### 5.3 Anomaly Detection

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| VR-008 | Detect high cost anomaly | 1. Record with cost > threshold<br>2. Run anomaly detection | HasAnomalies = true. AnomalyTypes includes "HighCost". |
| VR-009 | Detect unusual duration | 1. Call duration > 2 hours<br>2. Run anomaly detection | HasAnomalies = true. AnomalyTypes includes "LongDuration". |
| VR-010 | Detect off-hours call | 1. Call at 2 AM<br>2. Run anomaly detection | HasAnomalies = true. AnomalyTypes includes "OffHours". |
| VR-011 | No anomalies detected | 1. Normal call within thresholds<br>2. Run detection | HasAnomalies = false. AnomalyTypes empty. |

### 5.4 Verification Status Filtering

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| VR-012 | Filter by Pending | 1. Select "Pending" status filter<br>2. Apply | Only pending records shown. |
| VR-013 | Filter by Verified | 1. Select "Verified" filter | Only verified records shown. |
| VR-014 | Filter by Rejected | 1. Select "Rejected" filter | Only rejected records with reasons shown. |
| VR-015 | Filter by Has Anomalies | 1. Check "Has Anomalies" filter | Only records with anomalies displayed. |

---

## 6. Push to Production

### 6.1 Push to Production Workflow

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| PP-001 | Push fully verified batch | 1. Ensure all records verified<br>2. Set verification deadline<br>3. Click "Push to Production" | Background job queued. CallRecord entries created. BatchStatus = "Published". |
| PP-002 | Attempt push with pending records | 1. Batch has pending records<br>2. Click Push | Error: "All records must be verified before pushing to production" |
| PP-003 | Set verification deadline | 1. Select deadline date (future)<br>2. Push to production | CallRecord.VerificationPeriod set to selected date. |
| PP-004 | Automatic approval deadline calculation | 1. Push with verification deadline<br>2. Check CallRecords | ApprovalPeriod = VerificationPeriod + configured approval days. |
| PP-005 | Push with notifications enabled | 1. Check "Send Notifications"<br>2. Push | Emails sent to all affected staff with call details and deadlines. |
| PP-006 | Push without notifications | 1. Uncheck "Send Notifications"<br>2. Push | Records pushed. No emails sent. |

### 6.2 Source Record Updates

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| PP-007 | Source records marked completed | 1. Push batch to production<br>2. Check Safaricom/Airtel/PSTN/PrivateWire tables | ProcessingStatus = "Completed" for all pushed records. |
| PP-008 | Staging batch status update | 1. Push completes<br>2. Check StagingBatch | BatchStatus = "Published". PublishedDate set. PublishedBy set. |

### 6.3 Deadline Management

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| PP-009 | View deadlines for published batch | 1. Select published batch<br>2. View deadline info | Verification deadline and Approval deadline displayed. |
| PP-010 | Deadline passed - verification | 1. Wait for verification deadline to pass<br>2. Check records | Records past deadline flagged. Recovery process applicable. |

---

## 7. Staff Call Verification (User Self-Service)

> **Page:** `/Modules/EBillManagement/CallRecords/MyCallLogs`
>
> This is the staff-facing workflow where users verify their own call records as Personal or Official.

### 7.1 View My Call Logs

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| SV-001 | Staff views their call logs | 1. Login as staff member<br>2. Navigate to E-Bill > My Call Logs | Call records displayed grouped by extension. Summary stats shown: Total calls, Verified, Unverified, Amount. |
| SV-002 | View with no linked profile | 1. Login as user without EbillUser record<br>2. Navigate to My Call Logs | Warning: "Your profile is not linked to an Staff record. Please contact the administrator." |
| SV-003 | Filter by month/year | 1. Select specific month and year<br>2. Apply filter | Only records for selected month/year displayed. Summary recalculated. |
| SV-004 | Filter by verification status | 1. Select "Unverified" status filter<br>2. Apply | Only unverified records shown. |
| SV-005 | Filter by minimum cost | 1. Set minimum cost filter (e.g., $5.00)<br>2. Apply | Only calls with cost >= $5.00 shown. |
| SV-006 | View assigned calls | 1. Have calls assigned from another user<br>2. Filter by "Assigned to me" | Shows calls assigned to current user by others. |
| SV-007 | View hierarchical grouping | 1. View call logs<br>2. Expand extension group<br>3. Expand dialed number | Three-level hierarchy: Extension → Dialed Number → Individual Calls. |

### 7.2 Verify Individual Calls

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| SV-008 | Mark call as Personal | 1. Select unverified call<br>2. Click "Personal" button<br>3. Confirm | Call marked IsVerified=true, VerificationType="Personal". |
| SV-009 | Mark call as Official | 1. Select unverified call<br>2. Click "Official" button<br>3. Confirm | Call marked IsVerified=true, VerificationType="Official". |
| SV-010 | Verify with justification | 1. Select call flagged for justification<br>2. Enter justification text<br>3. Submit | Justification saved. Call verified. |
| SV-011 | Attempt to re-verify already verified | 1. Find already verified call<br>2. Try to verify again | Call remains as-is or shows appropriate message that it's already verified. |

### 7.3 Quick Verification (Bulk)

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| SV-012 | Quick verify selected as Personal | 1. Select multiple unverified calls (checkbox)<br>2. Click "Mark as Personal"<br>3. Confirm | All selected calls marked as Personal. Success message: "Successfully marked X of Y call(s) as Personal". |
| SV-013 | Quick verify selected as Official | 1. Select multiple unverified calls<br>2. Click "Mark as Official"<br>3. Confirm | All selected calls marked as Official. |
| SV-014 | Quick verify with no selection | 1. Click "Mark as Personal" without selecting any calls | Warning: "Please select at least one call to verify". |
| SV-015 | Verify all by extension | 1. Select extension group<br>2. Click "Verify All as Official"<br>3. Confirm | All calls for that extension verified as Official. |
| SV-016 | Verify all by extension and month | 1. Filter to specific month<br>2. Select extension<br>3. "Verify All" | All calls for extension in that month verified. |

### 7.4 Allowance and Overage Tracking

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| SV-017 | View allowance limit | 1. Staff has Class of Service with limit<br>2. View My Call Logs | Allowance card shows: Limit ($X), Current Usage ($Y), Remaining ($Z). |
| SV-018 | View unlimited allowance | 1. Staff has unlimited Class of Service<br>2. View My Call Logs | Shows "Unlimited" allowance. No overage warnings. |
| SV-019 | Overage warning displayed | 1. Current usage exceeds allowance limit<br>2. View My Call Logs | Warning displayed: "You have exceeded your allowance by $X". Overage amount highlighted in red. |
| SV-020 | Submit overage justification | 1. Have overage on a phone<br>2. Click "Justify Overage"<br>3. Enter justification and upload document<br>4. Submit | Overage justification created with "Pending" status. Document saved. |

### 7.5 Submit to Supervisor

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| SV-021 | Submit verified calls to supervisor | 1. Verify multiple calls<br>2. Click "Submit to Supervisor"<br>3. Confirm | CallLogVerification records updated: SubmittedToSupervisor=true, SupervisorIndexNumber set. |
| SV-022 | Submit with overage requiring justification | 1. Have overage calls<br>2. Submit without overage justification | Error: "Please provide overage justification before submitting" or prompt to justify. |
| SV-023 | Submit mixed Personal/Official | 1. Verify some calls as Personal, some as Official<br>2. Submit all | All verified calls submitted. Supervisor sees breakdown. |
| SV-024 | View submission status | 1. Submit calls<br>2. View My Call Logs | Submitted calls show "Pending Approval" status. Cannot re-submit until processed. |

### 7.6 Deadline Handling

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| SV-025 | View verification deadline | 1. Have call records with VerificationPeriod set<br>2. View My Call Logs | Deadline displayed: "Verify by [Date]". |
| SV-026 | Deadline approaching warning | 1. Deadline within 3 days<br>2. View My Call Logs | Warning: "Verification deadline approaching. Please verify your calls by [Date]". |
| SV-027 | Verify after deadline passed | 1. Verification deadline has passed<br>2. Attempt to verify | Depending on system config: Either blocked with message or allowed with warning that recovery may apply. |

---

## 8. Supervisor Approval Workflow

> **Page:** `/Modules/EBillManagement/CallRecords/SupervisorApprovals`
>
> This is where supervisors review and approve/reject call verifications submitted by their staff.

### 8.1 View Pending Approvals

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| SA-001 | Supervisor views pending submissions | 1. Login as supervisor<br>2. Navigate to E-Bill > Supervisor Approvals | List of staff with pending submissions displayed. Stats: Total pending, Approved today, Rejected today. |
| SA-002 | View submissions grouped by staff | 1. View pending approvals | Submissions grouped by staff member showing: Name, Total Calls, Total Amount, Submission Date. |
| SA-003 | Expand staff submission details | 1. Click on staff member's submission<br>2. View details | Individual call records displayed with: Extension, Date, Dialed Number, Duration, Cost, Type (Personal/Official). |
| SA-004 | Filter by staff member | 1. Select specific staff from dropdown<br>2. Apply filter | Only selected staff's pending submissions shown. |
| SA-005 | Filter by month/year | 1. Select billing month/year<br>2. Apply | Only submissions for that period shown. |
| SA-006 | Filter overage only | 1. Check "Show only overage"<br>2. Apply | Only submissions with overage amounts displayed. |
| SA-007 | View with no pending approvals | 1. No staff have submitted<br>2. View page | Message: "No pending approvals. All submissions have been processed." |

### 8.2 Review Submission Details

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| SA-008 | View call details | 1. Expand submission<br>2. Click on individual call | Modal/detail shows: Full call information, Verification type, Justification (if any). |
| SA-009 | View overage justification | 1. Staff has overage with justification<br>2. View submission | Overage justification text and uploaded documents visible. |
| SA-010 | Download overage documents | 1. View submission with documents<br>2. Click download | Document downloaded successfully. |
| SA-011 | View extension overage status | 1. Submission has multiple extensions<br>2. View each extension's status | Per-extension breakdown: Allowance limit, Current usage, Overage amount, Class of Service. |

### 8.3 Approve Submissions

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| SA-012 | Approve single verification | 1. Select one call record<br>2. Click "Approve"<br>3. Confirm | SupervisorApprovalStatus="Approved". ApprovalDate set. Notification sent to staff. |
| SA-013 | Approve all for staff member | 1. Select all calls for a staff<br>2. Click "Approve Selected"<br>3. Confirm | All selected verifications approved. Success message with count. |
| SA-014 | Approve with comment | 1. Select call<br>2. Add approval comment<br>3. Approve | Approval saved with supervisor comment. |
| SA-015 | Approve overage submission | 1. Staff has overage with valid justification<br>2. Review justification<br>3. Approve | Overage approved. No recovery triggered for approved calls. |
| SA-016 | Bulk approve all pending | 1. Multiple staff have submissions<br>2. Select all<br>3. "Approve All" | All pending submissions approved in batch. |

### 8.4 Reject Submissions

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| SA-017 | Reject single verification | 1. Select call record<br>2. Enter rejection reason<br>3. Click "Reject" | SupervisorApprovalStatus="Rejected". RejectionReason saved. Staff notified. |
| SA-018 | Reject without reason | 1. Select call<br>2. Try to reject without entering reason | Error: "Rejection reason is required". |
| SA-019 | Reject multiple selected | 1. Select multiple calls<br>2. Enter common rejection reason<br>3. Reject | All selected verifications rejected with same reason. |
| SA-020 | Reject overage justification | 1. Staff submitted overage justification<br>2. Justification insufficient<br>3. Reject with reason | Overage justification rejected. Staff must resubmit or amount recovered. |
| SA-021 | Partial approval (some approve, some reject) | 1. Staff submitted 10 calls<br>2. Approve 7, reject 3<br>3. Submit | 7 approved, 3 rejected. Both outcomes processed correctly. |

### 8.5 Reassignment and Return

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| SA-022 | Return to staff for correction | 1. Select call with issues<br>2. Click "Return for Correction"<br>3. Enter reason | Call returned to staff queue. Staff can modify and resubmit. |
| SA-023 | Reassign to different supervisor | 1. Select submission<br>2. Click "Reassign"<br>3. Select new supervisor | Submission moved to new supervisor's queue. Original supervisor no longer sees it. |

### 8.6 Notifications and Email

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| SA-024 | Email sent on approval | 1. Approve submission<br>2. Check email log | Staff receives email: "Your call verification has been approved by [Supervisor]". |
| SA-025 | Email sent on rejection | 1. Reject submission<br>2. Check email log | Staff receives email with rejection reason and instructions to resubmit. |
| SA-026 | Dashboard shows pending count | 1. Have pending submissions<br>2. View dashboard/sidebar | Badge shows count of pending approvals. |

### 8.7 Deadline Handling

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| SA-027 | View approval deadline | 1. Submissions have ApprovalPeriod set<br>2. View pending | Deadline displayed for each submission. |
| SA-028 | Deadline approaching highlight | 1. Deadline within 2 days<br>2. View pending | Submission highlighted in orange/yellow with urgency indicator. |
| SA-029 | Approve after deadline | 1. Approval deadline passed<br>2. Try to approve | Depending on config: Blocked or allowed with warning that recovery may have started. |

---

## 9. Recovery Process (EOS - End of Service)

### 9.1 EOS Staff Identification

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| EOS-001 | View EOS staff list | 1. Navigate to Admin > EOS Recovery<br>2. View list | All staff with pending recovery displayed with amounts. |
| EOS-002 | Filter by organization | 1. Select organization filter<br>2. Apply | Only staff from selected organization shown. |
| EOS-003 | Filter by month/year | 1. Select billing period<br>2. Apply | Only staff with records in that period shown. |
| EOS-004 | Search by name/index | 1. Enter search term<br>2. Search | Matching staff displayed. |
| EOS-005 | View recovery statistics | 1. Load EOS page<br>2. View statistics panel | Total staff, Total recoverable amount, Breakdown by type displayed. |

### 9.2 Recovery Execution

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| EOS-006 | Trigger recovery for single staff | 1. Select one staff member<br>2. Click "Trigger Recovery" | Personal approved calls marked as RecoveryStatus = "Completed". Recovery transaction created. |
| EOS-007 | Trigger recovery for multiple staff | 1. Select multiple staff<br>2. Click "Trigger Recovery" | All selected staff processed. Individual recovery transactions created. |
| EOS-008 | Recovery only processes Personal calls | 1. Staff has Personal and Official approved calls<br>2. Trigger recovery | Only Personal calls recovered. Official calls remain unchanged (no cost recovery). |
| EOS-009 | Recovery calculation accuracy | 1. Staff has 5 personal calls totaling $50<br>2. Trigger recovery | RecoveryAmount = $50. Each call marked completed. |
| EOS-010 | Recovery with no pending records | 1. Staff has no pending recovery records<br>2. Trigger recovery | Message: "No pending recovery records for selected staff" |

### 9.3 Recovery Status Tracking

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| EOS-011 | View recovery status in staging | 1. Navigate to Call Log Staging<br>2. View recovery status column | Shows current RecoveryStatus for each record. |
| EOS-012 | Recovery date tracking | 1. After recovery<br>2. Check record details | RecoveryDate populated with execution timestamp. |

### 9.4 Recovery Reports

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| EOS-013 | View recovery summary | 1. Navigate to Admin > Recovery Reports<br>2. View summary | Total recovered, by category, by period displayed. |
| EOS-014 | Filter by date range | 1. Select date range<br>2. Apply | Only recoveries within range shown. |
| EOS-015 | Export recovery report | 1. Apply filters<br>2. Click Export | CSV/Excel file downloaded with recovery details. |
| EOS-016 | View per-staff breakdown | 1. Expand staff details<br>2. View individual records | All recovered calls for staff listed with amounts. |

### 9.5 Recovery Types

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| EOS-017 | Expired Verification recovery | 1. Staff didn't verify within deadline<br>2. System triggers recovery | RecoveryType = "Expired Verification". Full amount recovered. |
| EOS-018 | Expired Approval recovery | 1. Supervisor didn't approve within deadline<br>2. System triggers recovery | RecoveryType = "Expired Approval". Full amount recovered. |
| EOS-019 | Reverted call recovery | 1. Previously approved call reverted<br>2. System triggers recovery | RecoveryType = "Reverted". Amount recovered. |

---

## 10. End-to-End Integration Tests

### 10.1 Complete Workflow - Happy Path

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| E2E-001 | Full cycle - Safaricom (Manual) | 1. Create E-Bill User with phone<br>2. Upload Safaricom data with that phone<br>3. Create consolidation batch<br>4. Verify all records<br>5. Push to production<br>6. Staff marks as Personal<br>7. Supervisor approves<br>8. Recovery triggered | Complete cycle works. Recovery amount matches call costs. |
| E2E-002 | Full cycle - All providers (Manual) | 1. Create user with 4 phones<br>2. Upload data to all 4 providers<br>3. Consolidate<br>4. Verify and push<br>5. Process recovery | All provider data consolidated correctly. Recovery accurate. |
| E2E-003 | Full cycle - Billing Processing (Simplified) | 1. Create E-Bill User with phone<br>2. Upload data to all 4 providers<br>3. Navigate to Billing Processing<br>4. Preview records<br>5. Start Processing<br>6. Wait for completion<br>7. Send notifications<br>8. Trigger EOS recovery | Automated workflow completes. Records pushed to production. Notifications sent. |
| E2E-004 | Interim EOS workflow | 1. Create user<br>2. Upload 3 months of data<br>3. User marked for separation<br>4. Create interim batch<br>5. Verify and push<br>6. Trigger EOS recovery | Interim batch correctly scoped. Recovery for separating staff complete. |

### 10.2 Error Handling

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| E2E-005 | Recovery after failed push | 1. Push fails mid-process<br>2. Fix issue<br>3. Retry push<br>4. Complete recovery | System recovers gracefully. No duplicate records. |
| E2E-006 | Duplicate phone assignment handling | 1. Phone assigned to User A<br>2. Upload calls for that phone<br>3. Reassign phone to User B<br>4. Consolidate | Original user's records preserved. New assignment used for new records. |
| E2E-007 | Large batch processing | 1. Upload 1M records<br>2. Create consolidation batch<br>3. Verify all<br>4. Push to production | System handles large volume. No timeouts. Memory efficient. |

### 10.3 Concurrent Operations

| Test ID | Test Scenario | Steps | Expected Result |
|---------|---------------|-------|-----------------|
| E2E-008 | Multiple simultaneous imports | 1. Start Safaricom import<br>2. Immediately start Airtel import<br>3. Both run concurrently | Both complete successfully. No conflicts. |
| E2E-009 | Verification during push | 1. Start push to production<br>2. Try to verify another record | Appropriate locking. Error message for locked batch. |

---

## Test Data Requirements

### Sample E-Bill Users
```
IndexNumber: 12345, Name: John Doe, Email: john.doe@un.org, Phone: +254712345678
IndexNumber: 12346, Name: Jane Smith, Email: jane.smith@un.org, Phone: +254723456789
IndexNumber: 12347, Name: Bob Wilson, Email: bob.wilson@un.org, Phone: +254734567890
```

### Sample Call Log CSV (Safaricom)
```csv
CallingNumber,CalledNumber,CallDate,CallTime,Duration,Cost,CallType
+254712345678,+254700000001,2025-01-15,09:30:00,120,2.50,Voice
+254712345678,+254700000002,2025-01-15,14:45:00,300,5.00,Voice
```

### Sample Call Log CSV (Airtel)
```csv
MSISDN,DialedNumber,DateTime,DurationSeconds,Amount,ServiceType
254723456789,254711111111,2025-01-16 10:00:00,180,3.00,VOICE
```

---

## Acceptance Criteria

1. **User Management**: All CRUD operations work correctly with proper validation
2. **Data Upload**: All 4 providers supported with proper transformations
3. **Consolidation**: Source data correctly merged with phone lookups
4. **Verification**: Anomaly detection works; bulk operations efficient
5. **Production Push**: Deadlines calculated correctly; notifications sent
6. **Recovery**: Only Personal calls recovered; amounts accurate; audit trail complete

---

## Notes

- All monetary values are in USD
- Phone numbers should be normalized to E.164 format
- Dates should follow the configured format (system supports multiple formats)
- Background jobs monitored via Hangfire dashboard at `/hangfire`
- All operations logged for audit purposes
