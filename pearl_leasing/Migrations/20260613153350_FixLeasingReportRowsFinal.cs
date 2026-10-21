using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pearl_leasing.Migrations
{
    /// <inheritdoc />
    public partial class FixLeasingReportRowsFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_LeasingReportRows",
                table: "LeasingReportRows");

            migrationBuilder.DropColumn(
                name: "BuildingType",
                table: "LeasingReportRows");

            migrationBuilder.DropColumn(
                name: "StallUnit",
                table: "LeasingReportRows");

            migrationBuilder.AddColumn<int>(
                name: "BuildingId",
                table: "LeasingReportRows",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StallUnitId",
                table: "LeasingReportRows",
                type: "integer",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "LeasingReportRows_pkey",
                table: "LeasingReportRows",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "LeasingReportRows_pkey",
                table: "LeasingReportRows");

            migrationBuilder.DropColumn(
                name: "BuildingId",
                table: "LeasingReportRows");

            migrationBuilder.DropColumn(
                name: "StallUnitId",
                table: "LeasingReportRows");

            migrationBuilder.AddColumn<string>(
                name: "BuildingType",
                table: "LeasingReportRows",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StallUnit",
                table: "LeasingReportRows",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LeasingReportRows",
                table: "LeasingReportRows",
                column: "Id");
        }
    }
}
