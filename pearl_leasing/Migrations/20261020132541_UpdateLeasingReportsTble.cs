using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pearl_leasing.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLeasingReportsTble : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CashAdvance",
                table: "ExpenseReports");

            migrationBuilder.RenameColumn(
                name: "OtherUtilities",
                table: "ExpenseReports",
                newName: "MaintenanceExpenses");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "LeasingReports",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "LeasingReports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "LeasingReports",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "LeasingReports");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "LeasingReports");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "LeasingReports");

            migrationBuilder.RenameColumn(
                name: "MaintenanceExpenses",
                table: "ExpenseReports",
                newName: "OtherUtilities");

            migrationBuilder.AddColumn<decimal>(
                name: "CashAdvance",
                table: "ExpenseReports",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
