using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class ExpandBanAndAddLichSuBan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "BAN",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "KhuVuc",
                table: "BAN",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MaBanGop",
                table: "BAN",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucChua",
                table: "BAN",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "LICHSUBAN",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaBan = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    TrangThaiCu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TrangThaiMoi = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ThoiGian = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaNV = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LICHSUBAN", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LICHSUBAN_BAN_MaBan",
                        column: x => x.MaBan,
                        principalTable: "BAN",
                        principalColumn: "MaBan",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LICHSUBAN_NHANVIEN_MaNV",
                        column: x => x.MaNV,
                        principalTable: "NHANVIEN",
                        principalColumn: "MaNV",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LICHSUBAN_MaBan",
                table: "LICHSUBAN",
                column: "MaBan");

            migrationBuilder.CreateIndex(
                name: "IX_LICHSUBAN_MaNV",
                table: "LICHSUBAN",
                column: "MaNV");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LICHSUBAN");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "BAN");

            migrationBuilder.DropColumn(
                name: "KhuVuc",
                table: "BAN");

            migrationBuilder.DropColumn(
                name: "MaBanGop",
                table: "BAN");

            migrationBuilder.DropColumn(
                name: "SucChua",
                table: "BAN");
        }
    }
}
