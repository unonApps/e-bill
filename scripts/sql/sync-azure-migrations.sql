-- This script will insert all missing migrations into __EFMigrationsHistory
-- Run this in Azure Portal Query Editor AFTER checking which migrations are missing

-- Only insert migrations that don't already exist
IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20250620192830_InitialCreate')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20250620192830_InitialCreate', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20250620200642_AddUserStatus')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20250620200642_AddUserStatus', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20250620205129_AddClassOfService')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20250620205129_AddClassOfService', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20250620211421_AddHandsetAllowanceToClassOfService')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20250620211421_AddHandsetAllowanceToClassOfService', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20250620213518_AddServiceProvider')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20250620213518_AddServiceProvider', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20250620215436_AddRequestManagementTables')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20250620215436_AddRequestManagementTables', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20250620220910_UpdateSimRequestModel')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20250620220910_UpdateSimRequestModel', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20250620234433_AddSimRequestHistory')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20250620234433_AddSimRequestHistory', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20250621162443_AddClassOfServiceFieldsToSimRequest')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20250621162443_AddClassOfServiceFieldsToSimRequest', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20250621164056_AddSupervisorFieldsToRefundRequestAndEbill')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20250621164056_AddSupervisorFieldsToRefundRequestAndEbill', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20250621175847_AddIctsFieldsToSimRequest')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20250621175847_AddIctsFieldsToSimRequest', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20250622162737_AddPendingSIMCollectionStatus')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20250622162737_AddPendingSIMCollectionStatus', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20250622184626_UpdateRefundRequestWorkflow')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20250622184626_UpdateRefundRequestWorkflow', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20250622194507_UpdateRefundRequestModel')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20250622194507_UpdateRefundRequestModel', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20250623013242_AddCostAccountingFields')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20250623013242_AddCostAccountingFields', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20250623181816_AddClaimsUnitProcessingFields')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20250623181816_AddClaimsUnitProcessingFields', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20250703094239_AddEbillUserEntity')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20250703094239_AddEbillUserEntity', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20250710122644_AddCallLogEntity')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20250710122644_AddCallLogEntity', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20250710125334_AddImportAuditEntity')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20250710125334_AddImportAuditEntity', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20251002123541_AddVerificationPeriodToCallRecords')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20251002123541_AddVerificationPeriodToCallRecords', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20251002163017_AddCallLogVerificationSystemTables')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20251002163017_AddCallLogVerificationSystemTables', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20251002173558_AddUserPhoneRelationshipToCallRecords')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20251002173558_AddUserPhoneRelationshipToCallRecords', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20251003180350_AddPhoneStatusToUserPhone')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20251003180350_AddPhoneStatusToUserPhone', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20251003192422_AddEbillUserAuthentication')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20251003192422_AddEbillUserAuthentication', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20251006074150_ReplaceMonthlyCallCostLimitWithHandsetAllowanceAmount')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20251006074150_ReplaceMonthlyCallCostLimitWithHandsetAllowanceAmount', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20251006175733_AddAssignmentStatusToCallRecord')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20251006175733_AddAssignmentStatusToCallRecord', '8.0.0');

IF NOT EXISTS (SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20251008095859_AddNotificationsTable')
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20251008095859_AddNotificationsTable', '8.0.0');

-- Verify the sync worked
SELECT MigrationId, ProductVersion
FROM __EFMigrationsHistory
ORDER BY MigrationId;
