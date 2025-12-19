using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantManagementAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddAttributeToTableMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRead",
                table: "MESSAGES",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MaNV_Receiver",
                table: "MESSAGES",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MESSAGES_ConversationId_Timestamp",
                table: "MESSAGES",
                columns: new[] { "ConversationId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_MESSAGES_MaNV_Receiver",
                table: "MESSAGES",
                column: "MaNV_Receiver");

            migrationBuilder.AddForeignKey(
                name: "FK_MESSAGES_NHANVIEN_MaNV_Receiver",
                table: "MESSAGES",
                column: "MaNV_Receiver",
                principalTable: "NHANVIEN",
                principalColumn: "MaNV",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MESSAGES_NHANVIEN_MaNV_Receiver",
                table: "MESSAGES");

            migrationBuilder.DropIndex(
                name: "IX_MESSAGES_ConversationId_Timestamp",
                table: "MESSAGES");

            migrationBuilder.DropIndex(
                name: "IX_MESSAGES_MaNV_Receiver",
                table: "MESSAGES");

            migrationBuilder.DropColumn(
                name: "IsRead",
                table: "MESSAGES");

            migrationBuilder.DropColumn(
                name: "MaNV_Receiver",
                table: "MESSAGES");
        }
    }
}
