using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pearl_leasing.Migrations
{
    /// <inheritdoc />
    public partial class FixUserAndAdd2Fa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<string>(
            //    name: "PhoneNumber",
            //    table: "Users",
            //    type: "text",
            //    nullable: false,
            //    defaultValue: "");

            //migrationBuilder.AddColumn<bool>(
            //    name: "IsTwoFactorEnabled",
            //    table: "Users",
            //    type: "boolean",
            //    nullable: false,
            //    defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
