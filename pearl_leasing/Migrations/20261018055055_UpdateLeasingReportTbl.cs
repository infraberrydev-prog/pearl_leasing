using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pearl_leasing.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLeasingReportTbl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastDateUpdated",
                table: "LeasingReports",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastDateUpdated",
                table: "LeasingReports");
        }
    }
}
