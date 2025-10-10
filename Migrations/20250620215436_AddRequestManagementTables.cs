using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TAB.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestManagementTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ProcessedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ProcessingNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
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
                name: "RefundRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeviceModel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IMEINumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RefundAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RefundReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AdditionalDetails = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ProcessedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ProcessingNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApprovedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SimRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SimType = table.Column<int>(type: "int", nullable: false),
                    ServiceProviderId = table.Column<int>(type: "int", nullable: false),
                    ClassOfServiceId = table.Column<int>(type: "int", nullable: false),
                    Justification = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AdditionalNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ProcessedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ProcessingNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimRequests_ClassOfServices_ClassOfServiceId",
                        column: x => x.ClassOfServiceId,
                        principalTable: "ClassOfServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SimRequests_ServiceProviders_ServiceProviderId",
                        column: x => x.ServiceProviderId,
                        principalTable: "ServiceProviders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ebills_ServiceProviderId",
                table: "Ebills",
                column: "ServiceProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_SimRequests_ClassOfServiceId",
                table: "SimRequests",
                column: "ClassOfServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SimRequests_ServiceProviderId",
                table: "SimRequests",
                column: "ServiceProviderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Ebills");

            migrationBuilder.DropTable(
                name: "RefundRequests");

            migrationBuilder.DropTable(
                name: "SimRequests");
        }
    }
}
