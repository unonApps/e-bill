using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCallLogRecoveryAndReportingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalDeadline",
                table: "StagingBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecoveryProcessingDate",
                table: "StagingBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecoveryStatus",
                table: "StagingBatches",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalClassOfServiceAmount",
                table: "StagingBatches",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalOfficialAmount",
                table: "StagingBatches",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPersonalAmount",
                table: "StagingBatches",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalRecoveredAmount",
                table: "StagingBatches",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerificationDeadline",
                table: "StagingBatches",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "final_assignment_type",
                table: "CallRecords",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "recovery_amount",
                table: "CallRecords",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "recovery_date",
                table: "CallRecords",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recovery_processed_by",
                table: "CallRecords",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recovery_status",
                table: "CallRecords",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovalDeadline",
                table: "CallLogVerifications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BatchId",
                table: "CallLogVerifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DeadlineMissed",
                table: "CallLogVerifications",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RevertCount",
                table: "CallLogVerifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevertDeadline",
                table: "CallLogVerifications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmissionDeadline",
                table: "CallLogVerifications",
                type: "datetime2",
                nullable: true);

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
                    AutomationEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RequireApprovalForAutomation = table.Column<bool>(type: "bit", nullable: false),
                    NotificationEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ReminderDaysBefore = table.Column<int>(type: "int", nullable: true),
                    ConfigValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecoveryConfiguration", x => x.Id);
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

            migrationBuilder.CreateIndex(
                name: "IX_CallLogVerifications_BatchId",
                table: "CallLogVerifications",
                column: "BatchId");

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

            migrationBuilder.AddForeignKey(
                name: "FK_CallLogVerifications_StagingBatches_BatchId",
                table: "CallLogVerifications",
                column: "BatchId",
                principalTable: "StagingBatches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CallLogVerifications_StagingBatches_BatchId",
                table: "CallLogVerifications");

            migrationBuilder.DropTable(
                name: "DeadlineTracking");

            migrationBuilder.DropTable(
                name: "RecoveryConfiguration");

            migrationBuilder.DropTable(
                name: "RecoveryLogs");

            migrationBuilder.DropIndex(
                name: "IX_CallLogVerifications_BatchId",
                table: "CallLogVerifications");

            migrationBuilder.DropColumn(
                name: "ApprovalDeadline",
                table: "StagingBatches");

            migrationBuilder.DropColumn(
                name: "RecoveryProcessingDate",
                table: "StagingBatches");

            migrationBuilder.DropColumn(
                name: "RecoveryStatus",
                table: "StagingBatches");

            migrationBuilder.DropColumn(
                name: "TotalClassOfServiceAmount",
                table: "StagingBatches");

            migrationBuilder.DropColumn(
                name: "TotalOfficialAmount",
                table: "StagingBatches");

            migrationBuilder.DropColumn(
                name: "TotalPersonalAmount",
                table: "StagingBatches");

            migrationBuilder.DropColumn(
                name: "TotalRecoveredAmount",
                table: "StagingBatches");

            migrationBuilder.DropColumn(
                name: "VerificationDeadline",
                table: "StagingBatches");

            migrationBuilder.DropColumn(
                name: "final_assignment_type",
                table: "CallRecords");

            migrationBuilder.DropColumn(
                name: "recovery_amount",
                table: "CallRecords");

            migrationBuilder.DropColumn(
                name: "recovery_date",
                table: "CallRecords");

            migrationBuilder.DropColumn(
                name: "recovery_processed_by",
                table: "CallRecords");

            migrationBuilder.DropColumn(
                name: "recovery_status",
                table: "CallRecords");

            migrationBuilder.DropColumn(
                name: "ApprovalDeadline",
                table: "CallLogVerifications");

            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "CallLogVerifications");

            migrationBuilder.DropColumn(
                name: "DeadlineMissed",
                table: "CallLogVerifications");

            migrationBuilder.DropColumn(
                name: "RevertCount",
                table: "CallLogVerifications");

            migrationBuilder.DropColumn(
                name: "RevertDeadline",
                table: "CallLogVerifications");

            migrationBuilder.DropColumn(
                name: "SubmissionDeadline",
                table: "CallLogVerifications");
        }
    }
}
