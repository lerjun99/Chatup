using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatUp.Infrastructure.Migrations
{
    /// <summary>
    /// Bell notifications. Guarded SQL (safe to re-run) for the same reason as MultiSupportAutoReply: this database
    /// is also updated by hand. Same script: Migrations/Scripts/20261116_UserNotifications.sql
    /// </summary>
    public partial class UserNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(Scripts.Up);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF OBJECT_ID('dbo.UserNotifications', 'U') IS NOT NULL DROP TABLE dbo.UserNotifications;");
        }

        internal static class Scripts
        {
            public const string Up = @"
IF OBJECT_ID('dbo.UserNotifications', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserNotifications (
        Id          bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserNotifications PRIMARY KEY,
        UserId      int NOT NULL,
        Type        nvarchar(40) NOT NULL,
        Title       nvarchar(200) NOT NULL,
        Message     nvarchar(500) NOT NULL,
        TicketId    int NULL,
        ActorUserId int NULL,
        Count       int NOT NULL CONSTRAINT DF_UserNotifications_Count DEFAULT 1,
        IsRead      bit NOT NULL CONSTRAINT DF_UserNotifications_IsRead DEFAULT 0,
        CreatedAt   datetime2 NOT NULL,
        ReadAt      datetime2 NULL
    );
    CREATE INDEX IX_UserNotifications_UserId_IsRead_CreatedAt ON dbo.UserNotifications (UserId, IsRead, CreatedAt);
END;
";
        }
    }
}
