using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatUp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class newdbtb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
          

            migrationBuilder.CreateTable(
                name: "TicketUploads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Base64Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateUploaded = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedById = table.Column<int>(type: "int", nullable: false),
                    UploadedById1 = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketUploads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketUploads_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketUploads_UserAccounts_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketUploads_UserAccounts_UploadedById1",
                        column: x => x.UploadedById1,
                        principalTable: "UserAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

     
            migrationBuilder.CreateIndex(
                name: "IX_TicketUploads_TicketId",
                table: "TicketUploads",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketUploads_UploadedById",
                table: "TicketUploads",
                column: "UploadedById");

            migrationBuilder.CreateIndex(
                name: "IX_TicketUploads_UploadedById1",
                table: "TicketUploads",
                column: "UploadedById1");

           
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
           
            migrationBuilder.DropTable(
                name: "TicketUploads");

          
        }
    }
}
