using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pearl_leasing.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLeasingReportRowTbl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CusaFee",
                table: "LeasingReportRows",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PylonRental",
                table: "LeasingReportRows",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TinNo",
                table: "LeasingReportRows",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CusaFee",
                table: "LeasingReportRows");

            migrationBuilder.DropColumn(
                name: "PylonRental",
                table: "LeasingReportRows");

            migrationBuilder.DropColumn(
                name: "TinNo",
                table: "LeasingReportRows");
        }
    }
}
