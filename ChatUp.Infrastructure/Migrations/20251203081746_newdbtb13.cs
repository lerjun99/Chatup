using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatUp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class newdbtb13 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketUploads_UserAccounts_UploadedById1",
                table: "TicketUploads");

            migrationBuilder.DropIndex(
                name: "IX_TicketUploads_UploadedById1",
                table: "TicketUploads");

            migrationBuilder.DropColumn(
                name: "UploadedById1",
                table: "TicketUploads");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UploadedById1",
                table: "TicketUploads",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TicketUploads_UploadedById1",
                table: "TicketUploads",
                column: "UploadedById1");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketUploads_UserAccounts_UploadedById1",
                table: "TicketUploads",
                column: "UploadedById1",
                principalTable: "UserAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
