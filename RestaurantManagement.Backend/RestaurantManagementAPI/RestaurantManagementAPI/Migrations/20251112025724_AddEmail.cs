using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "TAIKHOAN",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "TAIKHOAN",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OTP",
                table: "TAIKHOAN",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OTPExpireTime",
                table: "TAIKHOAN",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "TAIKHOAN");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "TAIKHOAN");

            migrationBuilder.DropColumn(
                name: "OTP",
                table: "TAIKHOAN");

            migrationBuilder.DropColumn(
                name: "OTPExpireTime",
                table: "TAIKHOAN");
        }
    }
}
