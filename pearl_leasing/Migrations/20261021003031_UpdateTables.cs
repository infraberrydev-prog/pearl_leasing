using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pearl_leasing.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "ExpenseReports",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                table: "ExpenseReports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ExpenseReports",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastDateUpdated",
                table: "ExpenseReports",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "ExpenseReports");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "ExpenseReports");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ExpenseReports");

            migrationBuilder.DropColumn(
                name: "LastDateUpdated",
                table: "ExpenseReports");
        }
    }
}
