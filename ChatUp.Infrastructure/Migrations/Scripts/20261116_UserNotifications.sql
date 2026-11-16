-- ChatUp: bell notifications (same as EF migration 20261116053919_UserNotifications). Safe to run more than once.

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

