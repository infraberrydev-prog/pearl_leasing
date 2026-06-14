using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace pearl_leasing.Migrations
{
    /// <inheritdoc />
    public partial class AddLeasingReportTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeasingReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReportName = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GrandTotalAmount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeasingReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeasingReportRows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeasingReportId = table.Column<int>(type: "integer", nullable: false),
                    BuildingType = table.Column<string>(type: "text", nullable: false),
                    VendorNameOrCategory = table.Column<string>(type: "text", nullable: false),
                    StallUnit = table.Column<string>(type: "text", nullable: false),
                    NameOfLessee = table.Column<string>(type: "text", nullable: false),
                    MobileNumber = table.Column<string>(type: "text", nullable: true),
                    MonthlyRental = table.Column<decimal>(type: "numeric", nullable: false),
                    PaymentStatus = table.Column<string>(type: "text", nullable: false),
                    PaymentMethod = table.Column<string>(type: "text", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "text", nullable: false),
                    Electricity = table.Column<decimal>(type: "numeric", nullable: false),
                    Water = table.Column<decimal>(type: "numeric", nullable: false),
                    OtherUtilities = table.Column<decimal>(type: "numeric", nullable: false),
                    PenaltyTenPercent = table.Column<decimal>(type: "numeric", nullable: true),
                    RowTotalAmount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeasingReportRows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeasingReportRows_LeasingReports_LeasingReportId",
                        column: x => x.LeasingReportId,
                        principalTable: "LeasingReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeasingReportRows_LeasingReportId",
                table: "LeasingReportRows",
                column: "LeasingReportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeasingReportRows");

            migrationBuilder.DropTable(
                name: "LeasingReports");
        }
    }
}
