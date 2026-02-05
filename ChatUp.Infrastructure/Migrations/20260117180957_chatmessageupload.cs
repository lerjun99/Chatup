using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatUp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class chatmessageupload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChatMessageId",
                table: "UploadedFile",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UploadedFile_ChatMessageId",
                table: "UploadedFile",
                column: "ChatMessageId");

            migrationBuilder.AddForeignKey(
                name: "FK_UploadedFile_ChatMessages_ChatMessageId",
                table: "UploadedFile",
                column: "ChatMessageId",
                principalTable: "ChatMessages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UploadedFile_ChatMessages_ChatMessageId",
                table: "UploadedFile");

            migrationBuilder.DropIndex(
                name: "IX_UploadedFile_ChatMessageId",
                table: "UploadedFile");

            migrationBuilder.DropColumn(
                name: "ChatMessageId",
                table: "UploadedFile");
        }
    }
}
