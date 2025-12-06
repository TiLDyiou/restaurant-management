using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddGhiChuToChiTietHoaDon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GhiChu",
                table: "CHITIETHOADON",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GhiChu",
                table: "CHITIETHOADON");
        }
    }
}
