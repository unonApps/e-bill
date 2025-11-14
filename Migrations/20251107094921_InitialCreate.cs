using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnomalyTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    AutoReject = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnomalyTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PerformedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PerformedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IPAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Module = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AdditionalData = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BillingPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeriodCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MonthlyImportDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MonthlyBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MonthlyRecordCount = table.Column<int>(type: "int", nullable: false),
                    MonthlyTotalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InterimUpdateCount = table.Column<int>(type: "int", nullable: false),
                    LastInterimDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InterimRecordCount = table.Column<int>(type: "int", nullable: false),
                    InterimAdjustmentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ClosedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LockedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClassOfServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Class = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Service = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EligibleStaff = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AirtimeAllowance = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DataAllowance = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    HandsetAllowance = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    HandsetAIRemarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AirtimeAllowanceAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    DataAllowanceAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    HandsetAllowanceAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    BillingPeriod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ServiceStatus = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassOfServices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SmtpServer = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    SmtpPort = table.Column<int>(type: "int", nullable: false),
                    FromEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FromName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EnableSsl = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UseDefaultCredentials = table.Column<bool>(type: "bit", nullable: false),
                    Timeout = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TemplateCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HtmlBody = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlainTextBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AvailablePlaceholders = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsSystemTemplate = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ImportType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    TotalRecords = table.Column<int>(type: "int", nullable: false),
                    SuccessCount = table.Column<int>(type: "int", nullable: false),
                    SkippedCount = table.Column<int>(type: "int", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    UpdatedCount = table.Column<int>(type: "int", nullable: false),
                    ImportDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImportedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProcessingTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    DetailedResults = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SummaryMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ImportOptions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateFormatPreferences = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecoveryConfiguration",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RuleType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    DefaultVerificationDays = table.Column<int>(type: "int", nullable: true),
                    DefaultApprovalDays = table.Column<int>(type: "int", nullable: true),
                    DefaultRevertDays = table.Column<int>(type: "int", nullable: true),
                    MaxRevertsAllowed = table.Column<int>(type: "int", nullable: false),
                    JobIntervalMinutes = table.Column<int>(type: "int", nullable: true),
                    AutomationEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RequireApprovalForAutomation = table.Column<bool>(type: "bit", nullable: false),
                    NotificationEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ReminderDaysBefore = table.Column<int>(type: "int", nullable: true),
                    EnableEmailNotifications = table.Column<bool>(type: "bit", nullable: false),
                    AdminNotificationEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ConfigValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoveryConfiguration", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecoveryJobExecutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RunType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TriggeredBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExpiredVerificationsProcessed = table.Column<int>(type: "int", nullable: false),
                    ExpiredApprovalsProcessed = table.Column<int>(type: "int", nullable: false),
                    RevertedVerificationsProcessed = table.Column<int>(type: "int", nullable: false),
                    TotalRecordsProcessed = table.Column<int>(type: "int", nullable: false),
                    TotalAmountRecovered = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RemindersSent = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutionLog = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NextScheduledRun = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoveryJobExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefundRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PrimaryMobileNumber = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    IndexNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MobileNumberAssignedTo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OfficeExtension = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Office = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MobileService = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ClassOfService = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeviceAllowance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PreviousDeviceReimbursedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PurchaseReceiptPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PurchaseReceiptData = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    PurchaseReceiptFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PurchaseReceiptContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PurchaseReceiptUploadDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DevicePurchaseCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    DevicePurchaseAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Organization = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UmojaBankName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Supervisor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubmittedToSupervisor = table.Column<bool>(type: "bit", nullable: false),
                    SupervisorApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SupervisorNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SupervisorRemarks = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SupervisorName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SupervisorEmail = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    BudgetOfficerApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BudgetOfficerNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BudgetOfficerRemarks = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BudgetOfficerName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    BudgetOfficerEmail = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CostObject = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CostCenter = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FundCommitment = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StaffClaimsApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StaffClaimsNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StaffClaimsRemarks = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    StaffClaimsOfficerName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    StaffClaimsOfficerEmail = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    UmojaPaymentDocumentId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RefundUsdAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ClaimsActionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentApprovalNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentApprovalRemarks = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PaymentApproverName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PaymentApproverEmail = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CancellationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CancelledBy = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CompletionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompletionNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProcessedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceProviders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SPID = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ServiceProviderName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SPMainCP = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SPMainCPEmail = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SPOtherCPsEmail = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SPStatus = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceProviders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CallLogReconciliations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BillingPeriodId = table.Column<int>(type: "int", nullable: false),
                    SourceRecordId = table.Column<int>(type: "int", nullable: false),
                    SourceTable = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ImportType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ImportBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PreviousAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CurrentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AdjustmentReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsSuperseded = table.Column<bool>(type: "bit", nullable: false),
                    SupersededBy = table.Column<int>(type: "int", nullable: true),
                    SupersededDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallLogReconciliations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CallLogReconciliations_BillingPeriods_BillingPeriodId",
                        column: x => x.BillingPeriodId,
                        principalTable: "BillingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CallLogReconciliations_CallLogReconciliations_SupersededBy",
                        column: x => x.SupersededBy,
                        principalTable: "CallLogReconciliations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StagingBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BatchType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TotalRecords = table.Column<int>(type: "int", nullable: false),
                    VerifiedRecords = table.Column<int>(type: "int", nullable: false),
                    RejectedRecords = table.Column<int>(type: "int", nullable: false),
                    PendingRecords = table.Column<int>(type: "int", nullable: false),
                    RecordsWithAnomalies = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartProcessingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndProcessingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecoveryProcessingDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecoveryStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TotalRecoveredAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalPersonalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalOfficialAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalClassOfServiceAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BatchStatus = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VerifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PublishedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SourceSystems = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillingPeriodId = table.Column<int>(type: "int", nullable: true),
                    BatchCategory = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StagingBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StagingBatches_BillingPeriods_BillingPeriodId",
                        column: x => x.BillingPeriodId,
                        principalTable: "BillingPeriods",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmailLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ToEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CcEmails = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BccEmails = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlainTextBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailTemplateId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    MaxRetries = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    ScheduledSendDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OpenedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OpenCount = table.Column<int>(type: "int", nullable: false),
                    TrackingId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RelatedEntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailLogs_EmailTemplates_EmailTemplateId",
                        column: x => x.EmailTemplateId,
                        principalTable: "EmailTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Offices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Offices_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ebills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ServiceProviderId = table.Column<int>(type: "int", nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BillMonth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BillAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BillType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AdditionalNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Supervisor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ProcessedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ProcessingNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    SubmittedToSupervisor = table.Column<bool>(type: "bit", nullable: false),
                    SupervisorApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SupervisorNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SupervisorRemarks = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SupervisorName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SupervisorEmail = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ebills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ebills_ServiceProviders_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SimRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IndexNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Organization = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Office = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FunctionalTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    OfficeExtension = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    OfficialEmail = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    SimType = table.Column<int>(type: "int", nullable: false),
                    ServiceProviderId = table.Column<int>(type: "int", nullable: false),
                    Supervisor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PreviouslyAssignedLines = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ProcessedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ProcessingNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SubmittedToSupervisor = table.Column<bool>(type: "bit", nullable: false),
                    SupervisorApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SupervisorNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MobileService = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MobileServiceAllowance = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HandsetAllowance = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SupervisorRemarks = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SupervisorName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SupervisorEmail = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SimSerialNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ServiceRequestNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LineType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SimPuk = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LineUsage = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PreviousLines = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SpNotifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AssignedNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CollectionNotifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SimIssuedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SimCollectedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SimCollectedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IctsRemark = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimRequests_ServiceProviders_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DeadlineTracking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeadlineType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TargetEntity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeadlineDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExtendedDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeadlineStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MissedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecoveryProcessed = table.Column<bool>(type: "bit", nullable: false),
                    RecoveryProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtensionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExtensionApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExtensionApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeadlineTracking", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeadlineTracking_StagingBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "StagingBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InterimUpdates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BillingPeriodId = table.Column<int>(type: "int", nullable: false),
                    UpdateType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecordsAdded = table.Column<int>(type: "int", nullable: false),
                    RecordsModified = table.Column<int>(type: "int", nullable: false),
                    RecordsDeleted = table.Column<int>(type: "int", nullable: false),
                    NetAdjustmentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RequestedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovalDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Justification = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SupportingDocuments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessingNotes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterimUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterimUpdates_BillingPeriods_BillingPeriodId",
                        column: x => x.BillingPeriodId,
                        principalTable: "BillingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InterimUpdates_StagingBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "StagingBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmailAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmailLogId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailAttachments_EmailLogs_EmailLogId",
                        column: x => x.EmailLogId,
                        principalTable: "EmailLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubOffices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContactPerson = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OfficeId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubOffices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubOffices_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalTable: "Offices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SimRequestHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SimRequestId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PreviousStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NewStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PerformedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimRequestHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimRequestHistories_SimRequests_SimRequestId",
                        column: x => x.SimRequestId,
                        principalTable: "SimRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EbillUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IndexNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OfficialMobileNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IssuedDeviceID = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OrganizationId = table.Column<int>(type: "int", nullable: true),
                    OfficeId = table.Column<int>(type: "int", nullable: true),
                    SubOfficeId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SupervisorIndexNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SupervisorName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SupervisorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApplicationUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    HasLoginAccount = table.Column<bool>(type: "bit", nullable: false),
                    LoginEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EbillUsers", x => x.Id);
                    table.UniqueConstraint("AK_EbillUsers_IndexNumber", x => x.IndexNumber);
                    table.ForeignKey(
                        name: "FK_EbillUsers_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalTable: "Offices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EbillUsers_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EbillUsers_SubOffices_SubOfficeId",
                        column: x => x.SubOfficeId,
                        principalTable: "SubOffices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequirePasswordChange = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AzureAdObjectId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AzureAdTenantId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AzureAdUpn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EbillUserId = table.Column<int>(type: "int", nullable: true),
                    OrganizationId = table.Column<int>(type: "int", nullable: true),
                    OfficeId = table.Column<int>(type: "int", nullable: true),
                    SubOfficeId = table.Column<int>(type: "int", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_EbillUsers_EbillUserId",
                        column: x => x.EbillUserId,
                        principalTable: "EbillUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Offices_OfficeId",
                        column: x => x.OfficeId,
                        principalTable: "Offices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AspNetUsers_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AspNetUsers_SubOffices_SubOfficeId",
                        column: x => x.SubOfficeId,
                        principalTable: "SubOffices",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserPhones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IndexNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PhoneType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    LineType = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UnassignedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClassOfServiceId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPhones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPhones_ClassOfServices_ClassOfServiceId",
                        column: x => x.ClassOfServiceId,
                        principalTable: "ClassOfServices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserPhones_EbillUsers_IndexNumber",
                        column: x => x.IndexNumber,
                        principalTable: "EbillUsers",
                        principalColumn: "IndexNumber",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Link = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RelatedEntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Airtel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ext = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    call_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    call_time = table.Column<TimeSpan>(type: "time", nullable: true),
                    Dialed = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Dest = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Durx = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AmountUSD = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Dur = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    call_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    call_month = table.Column<int>(type: "int", nullable: true),
                    call_year = table.Column<int>(type: "int", nullable: true),
                    IndexNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserPhoneId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EbillUserId = table.Column<int>(type: "int", nullable: true),
                    ImportAuditId = table.Column<int>(type: "int", nullable: true),
                    ProcessingStatus = table.Column<int>(type: "int", nullable: false),
                    ProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StagingBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BillingPeriod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Airtel", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Airtel_EbillUsers_EbillUserId",
                        column: x => x.EbillUserId,
                        principalTable: "EbillUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Airtel_ImportAudits_ImportAuditId",
                        column: x => x.ImportAuditId,
                        principalTable: "ImportAudits",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Airtel_UserPhones_UserPhoneId",
                        column: x => x.UserPhoneId,
                        principalTable: "UserPhones",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CallLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubAccountNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubAccountName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MSISDN = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TaxInvoiceSummaryNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InvoiceNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NetAccessFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetUsageLessTax = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LessTaxes = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VAT16 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Excise15 = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GrossTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EbillUserId = table.Column<int>(type: "int", nullable: true),
                    UserPhoneId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImportedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImportedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CallLogs_EbillUsers_EbillUserId",
                        column: x => x.EbillUserId,
                        principalTable: "EbillUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CallLogs_UserPhones_UserPhoneId",
                        column: x => x.UserPhoneId,
                        principalTable: "UserPhones",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CallLogStagings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExtensionNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CallDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CallNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CallDestination = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CallEndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CallDuration = table.Column<int>(type: "int", nullable: false),
                    CallCurrencyCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CallCost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CallCostUSD = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CallCostKSHS = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CallType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CallDestinationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CallYear = table.Column<int>(type: "int", nullable: false),
                    CallMonth = table.Column<int>(type: "int", nullable: false),
                    ResponsibleIndexNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PayingIndexNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserPhoneId = table.Column<int>(type: "int", nullable: true),
                    BillingPeriodId = table.Column<int>(type: "int", nullable: true),
                    ImportType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsAdjustment = table.Column<bool>(type: "bit", nullable: false),
                    OriginalRecordId = table.Column<int>(type: "int", nullable: true),
                    AdjustmentReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceSystem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceRecordId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ImportedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ImportedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VerificationStatus = table.Column<int>(type: "int", nullable: false),
                    VerificationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VerificationNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasAnomalies = table.Column<bool>(type: "bit", nullable: false),
                    AnomalyTypes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AnomalyDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessingStatus = table.Column<int>(type: "int", nullable: false),
                    ProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ErrorDetails = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallLogStagings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CallLogStagings_BillingPeriods_BillingPeriodId",
                        column: x => x.BillingPeriodId,
                        principalTable: "BillingPeriods",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CallLogStagings_EbillUsers_PayingIndexNumber",
                        column: x => x.PayingIndexNumber,
                        principalTable: "EbillUsers",
                        principalColumn: "IndexNumber");
                    table.ForeignKey(
                        name: "FK_CallLogStagings_EbillUsers_ResponsibleIndexNumber",
                        column: x => x.ResponsibleIndexNumber,
                        principalTable: "EbillUsers",
                        principalColumn: "IndexNumber");
                    table.ForeignKey(
                        name: "FK_CallLogStagings_StagingBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "StagingBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CallLogStagings_UserPhones_UserPhoneId",
                        column: x => x.UserPhoneId,
                        principalTable: "UserPhones",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CallRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ext_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    call_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    call_number = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    call_destination = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    call_endtime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    call_duration = table.Column<int>(type: "int", nullable: false),
                    call_curr_code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    call_cost = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    call_cost_usd = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    call_cost_kshs = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    call_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    call_dest_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    call_year = table.Column<int>(type: "int", nullable: false),
                    call_month = table.Column<int>(type: "int", nullable: false),
                    ext_resp_index = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    call_pay_index = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    call_ver_ind = table.Column<bool>(type: "bit", nullable: false),
                    call_ver_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    verification_period = table.Column<DateTime>(type: "datetime2", nullable: true),
                    approval_period = table.Column<DateTime>(type: "datetime2", nullable: true),
                    revert_count = table.Column<int>(type: "int", nullable: false),
                    last_revert_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    revert_reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    verification_type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    payment_assignment_id = table.Column<int>(type: "int", nullable: true),
                    assignment_status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    overage_justified = table.Column<bool>(type: "bit", nullable: false),
                    supervisor_approval_status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    supervisor_approved_by = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    supervisor_approved_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    recovery_status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    recovery_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    recovery_processed_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    final_assignment_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    recovery_amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    call_cert_ind = table.Column<bool>(type: "bit", nullable: false),
                    call_cert_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    call_cert_by = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    call_proc_ind = table.Column<bool>(type: "bit", nullable: false),
                    entry_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    call_dest_descr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SourceSystem = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SourceBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceStagingId = table.Column<int>(type: "int", nullable: true),
                    UserPhoneId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CallRecords_EbillUsers_call_pay_index",
                        column: x => x.call_pay_index,
                        principalTable: "EbillUsers",
                        principalColumn: "IndexNumber");
                    table.ForeignKey(
                        name: "FK_CallRecords_EbillUsers_ext_resp_index",
                        column: x => x.ext_resp_index,
                        principalTable: "EbillUsers",
                        principalColumn: "IndexNumber");
                    table.ForeignKey(
                        name: "FK_CallRecords_UserPhones_UserPhoneId",
                        column: x => x.UserPhoneId,
                        principalTable: "UserPhones",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PhoneOverageJustifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserPhoneId = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    AllowanceLimit = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TotalUsage = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    OverageAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    JustificationText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ApprovedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneOverageJustifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneOverageJustifications_UserPhones_UserPhoneId",
                        column: x => x.UserPhoneId,
                        principalTable: "UserPhones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrivateWires",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Extension = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DestinationLine = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DurationExtended = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DialedNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CallTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    Destination = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AmountUSD = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    AmountKSH = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    CallDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CallMonth = table.Column<int>(type: "int", nullable: false),
                    CallYear = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    IndexNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserPhoneId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EbillUserId = table.Column<int>(type: "int", nullable: true),
                    ImportAuditId = table.Column<int>(type: "int", nullable: true),
                    ProcessingStatus = table.Column<int>(type: "int", nullable: false),
                    ProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StagingBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BillingPeriod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivateWires", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrivateWires_EbillUsers_EbillUserId",
                        column: x => x.EbillUserId,
                        principalTable: "EbillUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PrivateWires_ImportAudits_ImportAuditId",
                        column: x => x.ImportAuditId,
                        principalTable: "ImportAudits",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PrivateWires_UserPhones_UserPhoneId",
                        column: x => x.UserPhoneId,
                        principalTable: "UserPhones",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PSTNs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Extension = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DialedNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CallTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    Destination = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DestinationLine = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DurationExtended = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Duration = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CallDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CallMonth = table.Column<int>(type: "int", nullable: false),
                    CallYear = table.Column<int>(type: "int", nullable: false),
                    AmountKSH = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AmountUSD = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    IndexNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserPhoneId = table.Column<int>(type: "int", nullable: true),
                    Carrier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EbillUserId = table.Column<int>(type: "int", nullable: true),
                    ImportAuditId = table.Column<int>(type: "int", nullable: true),
                    ProcessingStatus = table.Column<int>(type: "int", nullable: false),
                    ProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StagingBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BillingPeriod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PSTNs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PSTNs_EbillUsers_EbillUserId",
                        column: x => x.EbillUserId,
                        principalTable: "EbillUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PSTNs_ImportAudits_ImportAuditId",
                        column: x => x.ImportAuditId,
                        principalTable: "ImportAudits",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PSTNs_UserPhones_UserPhoneId",
                        column: x => x.UserPhoneId,
                        principalTable: "UserPhones",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Safaricom",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Ext = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    call_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    call_time = table.Column<TimeSpan>(type: "time", nullable: true),
                    Dialed = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Dest = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Durx = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AmountUSD = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Dur = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    call_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    call_month = table.Column<int>(type: "int", nullable: true),
                    call_year = table.Column<int>(type: "int", nullable: true),
                    IndexNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserPhoneId = table.Column<int>(type: "int", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EbillUserId = table.Column<int>(type: "int", nullable: true),
                    ImportAuditId = table.Column<int>(type: "int", nullable: true),
                    ProcessingStatus = table.Column<int>(type: "int", nullable: false),
                    ProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StagingBatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BillingPeriod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Safaricom", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Safaricom_EbillUsers_EbillUserId",
                        column: x => x.EbillUserId,
                        principalTable: "EbillUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Safaricom_ImportAudits_ImportAuditId",
                        column: x => x.ImportAuditId,
                        principalTable: "ImportAudits",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Safaricom_UserPhones_UserPhoneId",
                        column: x => x.UserPhoneId,
                        principalTable: "UserPhones",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UserPhoneHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserPhoneId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FieldChanged = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OldValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ChangedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ChangedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IPAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPhoneHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPhoneHistories_UserPhones_UserPhoneId",
                        column: x => x.UserPhoneId,
                        principalTable: "UserPhones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CallLogPaymentAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CallRecordId = table.Column<int>(type: "int", nullable: false),
                    AssignedFrom = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssignedTo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AssignmentReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignmentStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AcceptedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NotificationSent = table.Column<bool>(type: "bit", nullable: false),
                    NotificationSentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NotificationViewedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallLogPaymentAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CallLogPaymentAssignments_CallRecords_CallRecordId",
                        column: x => x.CallRecordId,
                        principalTable: "CallRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecoveryLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CallRecordId = table.Column<int>(type: "int", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecoveryType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RecoveryAction = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RecoveryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecoveryReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AmountRecovered = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RecoveredFrom = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProcessedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DeadlineDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsAutomated = table.Column<bool>(type: "bit", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoveryLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecoveryLogs_CallRecords_CallRecordId",
                        column: x => x.CallRecordId,
                        principalTable: "CallRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecoveryLogs_StagingBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "StagingBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhoneOverageDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhoneOverageJustificationId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UploadedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UploadedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhoneOverageDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhoneOverageDocuments_PhoneOverageJustifications_PhoneOverageJustificationId",
                        column: x => x.PhoneOverageJustificationId,
                        principalTable: "PhoneOverageJustifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CallLogVerifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CallRecordId = table.Column<int>(type: "int", nullable: false),
                    VerifiedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VerifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerificationType = table.Column<int>(type: "int", nullable: false),
                    ClassOfServiceId = table.Column<int>(type: "int", nullable: true),
                    AllowanceAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ActualAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    IsOverage = table.Column<bool>(type: "bit", nullable: false),
                    OverageAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    OverageJustified = table.Column<bool>(type: "bit", nullable: false),
                    JustificationText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SupportingDocuments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaymentAssignmentId = table.Column<int>(type: "int", nullable: true),
                    ApprovalStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubmittedToSupervisor = table.Column<bool>(type: "bit", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SupervisorIndexNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SupervisorApprovalStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SupervisorApprovedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SupervisorApprovedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SupervisorComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApprovedAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubmissionDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeadlineMissed = table.Column<bool>(type: "bit", nullable: false),
                    RevertDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevertCount = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallLogVerifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CallLogVerifications_CallLogPaymentAssignments_PaymentAssignmentId",
                        column: x => x.PaymentAssignmentId,
                        principalTable: "CallLogPaymentAssignments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CallLogVerifications_CallRecords_CallRecordId",
                        column: x => x.CallRecordId,
                        principalTable: "CallRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CallLogVerifications_ClassOfServices_ClassOfServiceId",
                        column: x => x.ClassOfServiceId,
                        principalTable: "ClassOfServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CallLogVerifications_StagingBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "StagingBatches",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CallLogDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CallLogVerificationId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DocumentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UploadedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UploadedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallLogDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CallLogDocuments_CallLogVerifications_CallLogVerificationId",
                        column: x => x.CallLogVerificationId,
                        principalTable: "CallLogVerifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Airtel_EbillUserId",
                table: "Airtel",
                column: "EbillUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Airtel_ImportAuditId",
                table: "Airtel",
                column: "ImportAuditId");

            migrationBuilder.CreateIndex(
                name: "IX_Airtel_UserPhoneId",
                table: "Airtel",
                column: "UserPhoneId");

            migrationBuilder.CreateIndex(
                name: "IX_AnomalyTypes_Code",
                table: "AnomalyTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_EbillUserId",
                table: "AspNetUsers",
                column: "EbillUserId",
                unique: true,
                filter: "[EbillUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_OfficeId",
                table: "AspNetUsers",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_OrganizationId",
                table: "AspNetUsers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_SubOfficeId",
                table: "AspNetUsers",
                column: "SubOfficeId");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogDocuments_CallLogVerificationId",
                table: "CallLogDocuments",
                column: "CallLogVerificationId");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogPaymentAssignments_AssignedFrom_AssignedTo",
                table: "CallLogPaymentAssignments",
                columns: new[] { "AssignedFrom", "AssignedTo" });

            migrationBuilder.CreateIndex(
                name: "IX_CallLogPaymentAssignments_AssignedTo",
                table: "CallLogPaymentAssignments",
                column: "AssignedTo");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogPaymentAssignments_AssignmentStatus",
                table: "CallLogPaymentAssignments",
                column: "AssignmentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogPaymentAssignments_CallRecordId",
                table: "CallLogPaymentAssignments",
                column: "CallRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogReconciliations_BillingPeriodId",
                table: "CallLogReconciliations",
                column: "BillingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogReconciliations_SupersededBy",
                table: "CallLogReconciliations",
                column: "SupersededBy");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogs_EbillUserId",
                table: "CallLogs",
                column: "EbillUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogs_MSISDN",
                table: "CallLogs",
                column: "MSISDN");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogs_UserPhoneId",
                table: "CallLogs",
                column: "UserPhoneId");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogStagings_BatchId",
                table: "CallLogStagings",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogStagings_BillingPeriodId",
                table: "CallLogStagings",
                column: "BillingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogStagings_CallDate",
                table: "CallLogStagings",
                column: "CallDate");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogStagings_ExtensionNumber_CallDate_CallNumber",
                table: "CallLogStagings",
                columns: new[] { "ExtensionNumber", "CallDate", "CallNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_CallLogStagings_PayingIndexNumber",
                table: "CallLogStagings",
                column: "PayingIndexNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogStagings_ResponsibleIndexNumber",
                table: "CallLogStagings",
                column: "ResponsibleIndexNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogStagings_UserPhoneId",
                table: "CallLogStagings",
                column: "UserPhoneId");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogStagings_VerificationStatus",
                table: "CallLogStagings",
                column: "VerificationStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogVerifications_ApprovalStatus",
                table: "CallLogVerifications",
                column: "ApprovalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogVerifications_BatchId",
                table: "CallLogVerifications",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogVerifications_CallRecordId_VerifiedBy",
                table: "CallLogVerifications",
                columns: new[] { "CallRecordId", "VerifiedBy" });

            migrationBuilder.CreateIndex(
                name: "IX_CallLogVerifications_ClassOfServiceId",
                table: "CallLogVerifications",
                column: "ClassOfServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogVerifications_PaymentAssignmentId",
                table: "CallLogVerifications",
                column: "PaymentAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogVerifications_SupervisorIndexNumber",
                table: "CallLogVerifications",
                column: "SupervisorIndexNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogVerifications_VerifiedBy",
                table: "CallLogVerifications",
                column: "VerifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_CallRecords_call_date",
                table: "CallRecords",
                column: "call_date");

            migrationBuilder.CreateIndex(
                name: "IX_CallRecords_call_pay_index",
                table: "CallRecords",
                column: "call_pay_index");

            migrationBuilder.CreateIndex(
                name: "IX_CallRecords_call_year_call_month",
                table: "CallRecords",
                columns: new[] { "call_year", "call_month" });

            migrationBuilder.CreateIndex(
                name: "IX_CallRecords_ext_no",
                table: "CallRecords",
                column: "ext_no");

            migrationBuilder.CreateIndex(
                name: "IX_CallRecords_ext_resp_index",
                table: "CallRecords",
                column: "ext_resp_index");

            migrationBuilder.CreateIndex(
                name: "IX_CallRecords_UserPhoneId",
                table: "CallRecords",
                column: "UserPhoneId");

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineTracking_BatchId",
                table: "DeadlineTracking",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineTracking_DeadlineDate",
                table: "DeadlineTracking",
                column: "DeadlineDate");

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineTracking_DeadlineDate_DeadlineStatus",
                table: "DeadlineTracking",
                columns: new[] { "DeadlineDate", "DeadlineStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineTracking_DeadlineStatus",
                table: "DeadlineTracking",
                column: "DeadlineStatus");

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineTracking_DeadlineType_TargetEntity",
                table: "DeadlineTracking",
                columns: new[] { "DeadlineType", "TargetEntity" });

            migrationBuilder.CreateIndex(
                name: "IX_DeadlineTracking_TargetEntity",
                table: "DeadlineTracking",
                column: "TargetEntity");

            migrationBuilder.CreateIndex(
                name: "IX_Ebills_ServiceProviderId",
                table: "Ebills",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_EbillUsers_Email",
                table: "EbillUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EbillUsers_IndexNumber",
                table: "EbillUsers",
                column: "IndexNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EbillUsers_OfficeId",
                table: "EbillUsers",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_EbillUsers_OrganizationId",
                table: "EbillUsers",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_EbillUsers_SubOfficeId",
                table: "EbillUsers",
                column: "SubOfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailAttachments_EmailLogId",
                table: "EmailAttachments",
                column: "EmailLogId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailConfigurations_IsActive",
                table: "EmailConfigurations",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_CreatedDate",
                table: "EmailLogs",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_EmailTemplateId",
                table: "EmailLogs",
                column: "EmailTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_RelatedEntityType_RelatedEntityId",
                table: "EmailLogs",
                columns: new[] { "RelatedEntityType", "RelatedEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_SentDate",
                table: "EmailLogs",
                column: "SentDate");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_Status",
                table: "EmailLogs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_Status_CreatedDate",
                table: "EmailLogs",
                columns: new[] { "Status", "CreatedDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_ToEmail",
                table: "EmailLogs",
                column: "ToEmail");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_TrackingId",
                table: "EmailLogs",
                column: "TrackingId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_Category",
                table: "EmailTemplates",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_IsActive",
                table: "EmailTemplates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_IsSystemTemplate",
                table: "EmailTemplates",
                column: "IsSystemTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_TemplateCode",
                table: "EmailTemplates",
                column: "TemplateCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_Month_Year",
                table: "ExchangeRates",
                columns: new[] { "Month", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportAudits_ImportDate",
                table: "ImportAudits",
                column: "ImportDate");

            migrationBuilder.CreateIndex(
                name: "IX_ImportAudits_ImportType",
                table: "ImportAudits",
                column: "ImportType");

            migrationBuilder.CreateIndex(
                name: "IX_InterimUpdates_BatchId",
                table: "InterimUpdates",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_InterimUpdates_BillingPeriodId",
                table: "InterimUpdates",
                column: "BillingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Offices_OrganizationId",
                table: "Offices",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_Name",
                table: "Organizations",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhoneOverageDocuments_PhoneOverageJustificationId",
                table: "PhoneOverageDocuments",
                column: "PhoneOverageJustificationId");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneOverageJustifications_ApprovalStatus",
                table: "PhoneOverageJustifications",
                column: "ApprovalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneOverageJustifications_Month_Year",
                table: "PhoneOverageJustifications",
                columns: new[] { "Month", "Year" });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneOverageJustifications_UserPhoneId",
                table: "PhoneOverageJustifications",
                column: "UserPhoneId");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneOverageJustifications_UserPhoneId_Month_Year",
                table: "PhoneOverageJustifications",
                columns: new[] { "UserPhoneId", "Month", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrivateWires_EbillUserId",
                table: "PrivateWires",
                column: "EbillUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateWires_ImportAuditId",
                table: "PrivateWires",
                column: "ImportAuditId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivateWires_UserPhoneId",
                table: "PrivateWires",
                column: "UserPhoneId");

            migrationBuilder.CreateIndex(
                name: "IX_PSTNs_EbillUserId",
                table: "PSTNs",
                column: "EbillUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PSTNs_ImportAuditId",
                table: "PSTNs",
                column: "ImportAuditId");

            migrationBuilder.CreateIndex(
                name: "IX_PSTNs_UserPhoneId",
                table: "PSTNs",
                column: "UserPhoneId");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryConfiguration_IsEnabled",
                table: "RecoveryConfiguration",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryConfiguration_RuleName",
                table: "RecoveryConfiguration",
                column: "RuleName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryConfiguration_RuleType",
                table: "RecoveryConfiguration",
                column: "RuleType");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryLogs_BatchId",
                table: "RecoveryLogs",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryLogs_CallRecordId",
                table: "RecoveryLogs",
                column: "CallRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryLogs_RecoveredFrom",
                table: "RecoveryLogs",
                column: "RecoveredFrom");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryLogs_RecoveryDate",
                table: "RecoveryLogs",
                column: "RecoveryDate");

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryLogs_RecoveryDate_RecoveryType",
                table: "RecoveryLogs",
                columns: new[] { "RecoveryDate", "RecoveryType" });

            migrationBuilder.CreateIndex(
                name: "IX_RecoveryLogs_RecoveryType",
                table: "RecoveryLogs",
                column: "RecoveryType");

            migrationBuilder.CreateIndex(
                name: "IX_Safaricom_EbillUserId",
                table: "Safaricom",
                column: "EbillUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Safaricom_ImportAuditId",
                table: "Safaricom",
                column: "ImportAuditId");

            migrationBuilder.CreateIndex(
                name: "IX_Safaricom_UserPhoneId",
                table: "Safaricom",
                column: "UserPhoneId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProviders_SPID",
                table: "ServiceProviders",
                column: "SPID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SimRequestHistories_SimRequestId",
                table: "SimRequestHistories",
                column: "SimRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_SimRequestHistories_Timestamp",
                table: "SimRequestHistories",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_SimRequests_IndexNo",
                table: "SimRequests",
                column: "IndexNo");

            migrationBuilder.CreateIndex(
                name: "IX_SimRequests_ServiceProviderId",
                table: "SimRequests",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_StagingBatches_BatchStatus",
                table: "StagingBatches",
                column: "BatchStatus");

            migrationBuilder.CreateIndex(
                name: "IX_StagingBatches_BillingPeriodId",
                table: "StagingBatches",
                column: "BillingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_StagingBatches_CreatedDate",
                table: "StagingBatches",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_SubOffices_OfficeId",
                table: "SubOffices",
                column: "OfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPhoneHistories_UserPhoneId",
                table: "UserPhoneHistories",
                column: "UserPhoneId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPhones_ClassOfServiceId",
                table: "UserPhones",
                column: "ClassOfServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPhones_IndexNumber",
                table: "UserPhones",
                column: "IndexNumber");

            migrationBuilder.CreateIndex(
                name: "IX_UserPhones_IndexNumber_PhoneNumber_IsActive",
                table: "UserPhones",
                columns: new[] { "IndexNumber", "PhoneNumber", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_UserPhones_PhoneNumber",
                table: "UserPhones",
                column: "PhoneNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Airtel");

            migrationBuilder.DropTable(
                name: "AnomalyTypes");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CallLogDocuments");

            migrationBuilder.DropTable(
                name: "CallLogReconciliations");

            migrationBuilder.DropTable(
                name: "CallLogs");

            migrationBuilder.DropTable(
                name: "CallLogStagings");

            migrationBuilder.DropTable(
                name: "DeadlineTracking");

            migrationBuilder.DropTable(
                name: "Ebills");

            migrationBuilder.DropTable(
                name: "EmailAttachments");

            migrationBuilder.DropTable(
                name: "EmailConfigurations");

            migrationBuilder.DropTable(
                name: "ExchangeRates");

            migrationBuilder.DropTable(
                name: "InterimUpdates");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PhoneOverageDocuments");

            migrationBuilder.DropTable(
                name: "PrivateWires");

            migrationBuilder.DropTable(
                name: "PSTNs");

            migrationBuilder.DropTable(
                name: "RecoveryConfiguration");

            migrationBuilder.DropTable(
                name: "RecoveryJobExecutions");

            migrationBuilder.DropTable(
                name: "RecoveryLogs");

            migrationBuilder.DropTable(
                name: "RefundRequests");

            migrationBuilder.DropTable(
                name: "Safaricom");

            migrationBuilder.DropTable(
                name: "SimRequestHistories");

            migrationBuilder.DropTable(
                name: "UserPhoneHistories");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "CallLogVerifications");

            migrationBuilder.DropTable(
                name: "EmailLogs");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "PhoneOverageJustifications");

            migrationBuilder.DropTable(
                name: "ImportAudits");

            migrationBuilder.DropTable(
                name: "SimRequests");

            migrationBuilder.DropTable(
                name: "CallLogPaymentAssignments");

            migrationBuilder.DropTable(
                name: "StagingBatches");

            migrationBuilder.DropTable(
                name: "EmailTemplates");

            migrationBuilder.DropTable(
                name: "ServiceProviders");

            migrationBuilder.DropTable(
                name: "CallRecords");

            migrationBuilder.DropTable(
                name: "BillingPeriods");

            migrationBuilder.DropTable(
                name: "UserPhones");

            migrationBuilder.DropTable(
                name: "ClassOfServices");

            migrationBuilder.DropTable(
                name: "EbillUsers");

            migrationBuilder.DropTable(
                name: "SubOffices");

            migrationBuilder.DropTable(
                name: "Offices");

            migrationBuilder.DropTable(
                name: "Organizations");
        }
    }
}
