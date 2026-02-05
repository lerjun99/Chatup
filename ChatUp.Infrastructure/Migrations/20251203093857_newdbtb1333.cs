using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatUp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class newdbtb1333 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1️⃣ Add the column as nullable first to avoid conflicts with existing data
            migrationBuilder.AddColumn<int>(
                name: "TicketMessageId",
                table: "TicketUploads",
                type: "int",
                nullable: true); // allow null temporarily

            // 2️⃣ Set invalid or orphaned TicketMessageId to NULL
            // ⚠ Raw SQL to fix existing data
            migrationBuilder.Sql(@"
                UPDATE TicketUploads
                SET TicketMessageId = NULL
                WHERE TicketMessageId NOT IN (SELECT Id FROM TicketMessages)
            ");

            // 3️⃣ Create index
            migrationBuilder.CreateIndex(
                name: "IX_TicketUploads_TicketMessageId",
                table: "TicketUploads",
                column: "TicketMessageId");

            // 4️⃣ Add foreign key with Restrict delete behavior (no cascade)
            migrationBuilder.AddForeignKey(
                name: "FK_TicketUploads_TicketMessages_TicketMessageId",
                table: "TicketUploads",
                column: "TicketMessageId",
                principalTable: "TicketMessages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict); // ⚠ avoids multiple cascade paths
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketUploads_TicketMessages_TicketMessageId",
                table: "TicketUploads");

            migrationBuilder.DropIndex(
                name: "IX_TicketUploads_TicketMessageId",
                table: "TicketUploads");

            migrationBuilder.DropColumn(
                name: "TicketMessageId",
                table: "TicketUploads");
        }
    }
}
