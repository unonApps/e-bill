# EbillUsers CSV Import Guide

## Overview
This guide explains how to import EbillUsers from a CSV file using code-based mapping for Organizations, Offices, and SubOffices.

## Files Created
1. **prepare-codes-mapping.sql** - Ensures all organization/office codes exist in database
2. **import-ebillusers-csv.ps1** - PowerShell script to process CSV and generate SQL
3. **run-ebillusers-import.ps1** - Master script that runs the complete import process
4. **seed-ebillusers-from-csv.sql** - Manual SQL script for smaller imports

## Import Process

### Quick Start
Simply run the master script:
```powershell
.\run-ebillusers-import.ps1
```

### Step-by-Step Process

#### Step 1: Prepare Codes Mapping
```powershell
sqlcmd -S .\SQLEXPRESS -d TABDB -E -i prepare-codes-mapping.sql
```
This ensures all organization and office codes from the CSV exist in the database.

#### Step 2: Run Import
```powershell
.\import-ebillusers-csv.ps1 -CsvPath "C:\Users\dxmic\Downloads\ebill user.csv"
```
This reads the CSV, maps codes to IDs, and imports valid users.

## CSV Format
The CSV must have these columns:
- **OfficialMobileNumber** - User's mobile number
- **FirstName** - User's first name
- **LastName** - User's last name
- **IndexNumber** - Unique Staff ID
- **Location** - Physical location/building
- **Org** - Organization code (e.g., WHO, UNIC, UNON)
- **Office** - Office code (e.g., KCO, SOM, VS)
- **Sub-Office** - SubOffice code (usually empty)
- **ClassOfService** - Service class (0 = none)

## Code Mapping

### Organizations
The script maps organization codes like:
- **WHO** → World Health Organization
- **UNIC** → UN Information Centre
- **UNON** → UN Office at Nairobi
- **UNEP** → UN Environment Programme
- **FAO** → Food and Agriculture Organization
- **CON** → Consultants

### Offices
Office codes are mapped within their organization:
- **WHO/KCO** → Kenya Country Office
- **WHO/SOM** → Somalia Office
- **UNIC/VS** → Visitor Services
- **UNON/ICTS** → ICT Services

## Data Validation

### Records Skipped
The import skips records with:
- Special characters in names (-, &, $, #)
- Service accounts (Service, Reception, Library, Fax)
- Invalid IndexNumber (0 or empty)
- Test/temporary accounts

### Email Generation
Emails are auto-generated based on organization:
- WHO staff: firstname.lastname@who.int
- FAO staff: firstname.lastname@fao.org
- UN staff: firstname.lastname@un.org

## Troubleshooting

### Missing Organizations
If you see "Unmapped Organizations" in the output:
1. Add the organization to the database with its code
2. Re-run the import

### Missing Offices
If you see "Unmapped Offices" in the output:
1. Add the office to the correct organization with its code
2. Re-run the import

### Duplicate Records
The import automatically skips duplicates based on IndexNumber.

## Verification
After import, verify the data:
```sql
-- Check import counts
SELECT COUNT(*) as TotalUsers FROM EbillUsers;

-- Check mapping success
SELECT
    COUNT(*) as Total,
    COUNT(OrganizationId) as WithOrg,
    COUNT(OfficeId) as WithOffice,
    COUNT(Location) as WithLocation
FROM EbillUsers;

-- View recent imports
SELECT TOP 10 *
FROM EbillUsers
ORDER BY CreatedDate DESC;
```

## Notes
- The Location field is now included in the import
- ClassOfService = 0 is treated as NULL
- The script processes in batches of 100 records for safety
- All imports are logged with timestamps