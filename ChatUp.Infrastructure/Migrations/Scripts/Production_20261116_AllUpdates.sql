-- =====================================================================================================
-- ChatUp production update - 2026-11-16
-- Covers EF migrations:
--   20261116050634_MultiSupportAutoReply   (multi-support agents, chat auto reply)
--   20261116053919_UserNotifications       (bell notifications)
--   20261116065227_AdminActivityLogs       (audit log)
--
-- Safe to run more than once: every change is guarded, nothing existing is dropped except the
-- superseded index IX_UserClientAssignments_ClientId (replaced by IX_UserClientAssignments_ClientId_UserId).
-- Run in SSMS (or sqlcmd -i) against the production ChatUp database. Take a backup first.
-- =====================================================================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

PRINT '1/4  MultiSupportAutoReply';
GO

-- Ticket columns used by the inbox ordering / needs-reply filter (exist on most databases already)
IF COL_LENGTH('dbo.Tickets', 'LastClientMessageAt') IS NULL
    ALTER TABLE dbo.Tickets ADD LastClientMessageAt datetime2 NULL;
IF COL_LENGTH('dbo.Tickets', 'LastSupportReplyAt') IS NULL
    ALTER TABLE dbo.Tickets ADD LastSupportReplyAt datetime2 NULL;
IF COL_LENGTH('dbo.Tickets', 'HasUnreadSupportReply') IS NULL
    ALTER TABLE dbo.Tickets ADD HasUnreadSupportReply bit NOT NULL CONSTRAINT DF_Tickets_HasUnreadSupportReply DEFAULT 0;
IF COL_LENGTH('dbo.Tickets', 'SupportResponseReminderSent') IS NULL
    ALTER TABLE dbo.Tickets ADD SupportResponseReminderSent bit NOT NULL CONSTRAINT DF_Tickets_SupportResponseReminderSent DEFAULT 0;
IF COL_LENGTH('dbo.Tickets', 'SupportResponseReminderSentAt') IS NULL
    ALTER TABLE dbo.Tickets ADD SupportResponseReminderSentAt datetime2 NULL;

-- Auto-reply messages are flagged so they never count as a support response
IF COL_LENGTH('dbo.TicketMessages', 'IsAutoReply') IS NULL
    ALTER TABLE dbo.TicketMessages ADD IsAutoReply bit NOT NULL CONSTRAINT DF_TicketMessages_IsAutoReply DEFAULT 0;

-- Auto-reply settings: one row per client, ClientId NULL = default for all clients
IF OBJECT_ID('dbo.ChatAutoReplies', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatAutoReplies (
        Id               int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ChatAutoReplies PRIMARY KEY,
        ClientId         int NULL,
        IsEnabled        bit NOT NULL,
        InstantMessage   nvarchar(2000) NULL,
        StartTime        nvarchar(5) NOT NULL,
        EndTime          nvarchar(5) NOT NULL,
        ActiveDays       nvarchar(20) NULL,
        ScheduledMessage nvarchar(2000) NULL,
        UpdatedBy        int NULL,
        DateUpdated      datetime2 NULL,
        CONSTRAINT FK_ChatAutoReplies_Client_ClientId FOREIGN KEY (ClientId) REFERENCES dbo.Client (Id) ON DELETE CASCADE
    );
    CREATE INDEX IX_ChatAutoReplies_ClientId ON dbo.ChatAutoReplies (ClientId);
END;

-- Several support agents per client: tickets are scoped by (ClientId, UserId) lookups
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserClientAssignments_ClientId_UserId' AND object_id = OBJECT_ID('dbo.UserClientAssignments'))
    CREATE INDEX IX_UserClientAssignments_ClientId_UserId ON dbo.UserClientAssignments (ClientId, UserId);
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserClientAssignments_ClientId' AND object_id = OBJECT_ID('dbo.UserClientAssignments'))
    DROP INDEX IX_UserClientAssignments_ClientId ON dbo.UserClientAssignments;

GO

-- Older tickets predate these columns: fill them from the messages so they sort and filter correctly
UPDATE t SET t.LastClientMessageAt = m.LastAt
FROM dbo.Tickets t
CROSS APPLY (SELECT MAX(DateCreated) AS LastAt FROM dbo.TicketMessages WHERE TicketId = t.Id AND IsUser = 1) m
WHERE t.LastClientMessageAt IS NULL AND m.LastAt IS NOT NULL;

UPDATE t SET t.LastSupportReplyAt = m.LastAt
FROM dbo.Tickets t
CROSS APPLY (SELECT MAX(DateCreated) AS LastAt FROM dbo.TicketMessages WHERE TicketId = t.Id AND IsUser = 0 AND IsAutoReply = 0) m
WHERE t.LastSupportReplyAt IS NULL AND m.LastAt IS NOT NULL;

GO


PRINT '2/4  UserNotifications';
GO

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
GO


PRINT '3/4  AdminActivityLogs';
GO

IF OBJECT_ID('dbo.AdminActivityLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AdminActivityLogs (
        Id            bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_AdminActivityLogs PRIMARY KEY,
        OccurredAt    datetime2(3)     NOT NULL,
        CorrelationId uniqueidentifier NOT NULL,
        SessionId     varchar(64)      NULL,
        UserId        int              NULL,
        UserName      nvarchar(256)    NULL,
        Category      varchar(20)      NOT NULL,
        Action        varchar(50)      NOT NULL,
        Module        nvarchar(100)    NOT NULL,
        EntityName    nvarchar(100)    NULL,
        EntityId      nvarchar(100)    NULL,
        Summary       nvarchar(500)    NOT NULL,
        Details       nvarchar(max)    NULL,
        Changes       nvarchar(max)    NULL,
        Route         nvarchar(500)    NULL,
        HttpMethod    varchar(10)      NULL,
        StatusCode    int              NULL,
        DurationMs    int              NULL,
        IsSuccess     bit              NOT NULL,
        ErrorMessage  nvarchar(1000)   NULL,
        IpAddress     varchar(45)      NULL,
        UserAgent     nvarchar(400)    NULL,
        Source        varchar(20)      NULL
    );

    CREATE INDEX IX_AdminActivityLogs_OccurredAt ON dbo.AdminActivityLogs (OccurredAt);
    CREATE INDEX IX_AdminActivityLogs_UserName_OccurredAt ON dbo.AdminActivityLogs (UserName, OccurredAt);
    CREATE INDEX IX_AdminActivityLogs_Category_OccurredAt ON dbo.AdminActivityLogs (Category, OccurredAt);
    CREATE INDEX IX_AdminActivityLogs_EntityId ON dbo.AdminActivityLogs (EntityId);
    CREATE INDEX IX_AdminActivityLogs_SessionId ON dbo.AdminActivityLogs (SessionId);
    CREATE INDEX IX_AdminActivityLogs_CorrelationId ON dbo.AdminActivityLogs (CorrelationId);
END;
GO

-- Record the migrations so "dotnet ef database update" will not try to apply them again
PRINT '4/4  __EFMigrationsHistory';
GO
IF OBJECT_ID('dbo.__EFMigrationsHistory', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261116050634_MultiSupportAutoReply')
        INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20261116050634_MultiSupportAutoReply', '8.0.18');
    IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261116053919_UserNotifications')
        INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20261116053919_UserNotifications', '8.0.18');
    IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = '20261116065227_AdminActivityLogs')
        INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('20261116065227_AdminActivityLogs', '8.0.18');
END;
GO

-- Quick check: every row should say OK
SELECT 'Tickets.LastClientMessageAt' AS Item, CASE WHEN COL_LENGTH('dbo.Tickets','LastClientMessageAt') IS NOT NULL THEN 'OK' ELSE 'MISSING' END AS Status
UNION ALL SELECT 'TicketMessages.IsAutoReply', CASE WHEN COL_LENGTH('dbo.TicketMessages','IsAutoReply') IS NOT NULL THEN 'OK' ELSE 'MISSING' END
UNION ALL SELECT 'ChatAutoReplies',   CASE WHEN OBJECT_ID('dbo.ChatAutoReplies','U')   IS NOT NULL THEN 'OK' ELSE 'MISSING' END
UNION ALL SELECT 'UserNotifications', CASE WHEN OBJECT_ID('dbo.UserNotifications','U') IS NOT NULL THEN 'OK' ELSE 'MISSING' END
UNION ALL SELECT 'AdminActivityLogs', CASE WHEN OBJECT_ID('dbo.AdminActivityLogs','U') IS NOT NULL THEN 'OK' ELSE 'MISSING' END
UNION ALL SELECT 'IX_UserClientAssignments_ClientId_UserId',
    CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserClientAssignments_ClientId_UserId') THEN 'OK' ELSE 'MISSING' END;
GO
