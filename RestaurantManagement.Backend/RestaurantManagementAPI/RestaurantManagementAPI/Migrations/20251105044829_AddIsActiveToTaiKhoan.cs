using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddIsActiveToTaiKhoan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HoatDong",
                table: "TAIKHOAN",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HoatDong",
                table: "TAIKHOAN");
        }
    }
}
