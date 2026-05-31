using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddSequencesAndPaginationSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "MaDatBanSequence");

            migrationBuilder.CreateSequence<int>(
                name: "MaHDSequence");

            migrationBuilder.CreateSequence<int>(
                name: "MaNVSequence");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "MaDatBanSequence");

            migrationBuilder.DropSequence(
                name: "MaHDSequence");

            migrationBuilder.DropSequence(
                name: "MaNVSequence");
        }
    }
}
