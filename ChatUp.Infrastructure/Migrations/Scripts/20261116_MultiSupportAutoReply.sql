-- ChatUp: multi-support + chat auto reply (same as EF migration 20261116050634_MultiSupportAutoReply)
-- Safe to run more than once.

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

