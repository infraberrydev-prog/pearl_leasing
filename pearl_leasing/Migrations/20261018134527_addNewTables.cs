using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace pearl_leasing.Migrations
{
    /// <inheritdoc />
    public partial class addNewTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EntityName = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    ModifiedBy = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    OldValues = table.Column<string>(type: "jsonb", nullable: true),
                    NewValues = table.Column<string>(type: "jsonb", nullable: true),
                    ChangedColumns = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Logs_DeletedReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeasingReportRowId = table.Column<int>(type: "integer", nullable: false),
                    LeasingReportId = table.Column<int>(type: "integer", nullable: false),
                    BuildingId = table.Column<int>(type: "integer", nullable: true),
                    BuildingName = table.Column<string>(type: "text", nullable: false),
                    StallUnitId = table.Column<int>(type: "integer", nullable: true),
                    StallUnitNumber = table.Column<string>(type: "text", nullable: false),
                    VendorNameOrCategory = table.Column<string>(type: "text", nullable: false),
                    NameOfLessee = table.Column<string>(type: "text", nullable: false),
                    MobileNumber = table.Column<string>(type: "text", nullable: true),
                    TinNo = table.Column<string>(type: "text", nullable: true),
                    MonthlyRental = table.Column<decimal>(type: "numeric", nullable: false),
                    PaymentStatus = table.Column<string>(type: "text", nullable: false),
                    PaymentMethod = table.Column<string>(type: "text", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "text", nullable: false),
                    Electricity = table.Column<decimal>(type: "numeric", nullable: false),
                    Water = table.Column<decimal>(type: "numeric", nullable: false),
                    OtherUtilities = table.Column<decimal>(type: "numeric", nullable: false),
                    CusaFee = table.Column<decimal>(type: "numeric", nullable: true),
                    PylonRental = table.Column<decimal>(type: "numeric", nullable: true),
                    PenaltyTenPercent = table.Column<decimal>(type: "numeric", nullable: true),
                    RowTotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    DeletedBy = table.Column<string>(type: "text", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs_DeletedReports", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Logs_DeletedReports");
        }
    }
}
