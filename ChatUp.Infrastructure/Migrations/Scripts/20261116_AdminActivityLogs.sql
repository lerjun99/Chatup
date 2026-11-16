-- ChatUp: audit log (same as EF migration 20261116065227_AdminActivityLogs). Safe to run more than once.

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
