using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pearl_leasing.Migrations
{
    /// <inheritdoc />
    public partial class AddUserOtpForeignKeyConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_UserOtps",
                table: "UserOtps");

            migrationBuilder.AddPrimaryKey(
                name: "UserOtps_pkey",
                table: "UserOtps",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_UserOtps_UserId",
                table: "UserOtps",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserOtps_Users_UserId",
                table: "UserOtps",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserOtps_Users_UserId",
                table: "UserOtps");

            migrationBuilder.DropPrimaryKey(
                name: "UserOtps_pkey",
                table: "UserOtps");

            migrationBuilder.DropIndex(
                name: "IX_UserOtps_UserId",
                table: "UserOtps");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserOtps",
                table: "UserOtps",
                column: "Id");
        }
    }
}
