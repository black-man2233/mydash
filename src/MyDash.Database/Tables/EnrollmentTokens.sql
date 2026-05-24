CREATE TABLE [dbo].[EnrollmentTokens] (
    [Id]          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [ServerName]  NVARCHAR(128)    NOT NULL,
    [TokenHash]   NVARCHAR(512)    NOT NULL,
    [Tags]        NVARCHAR(1024)   NOT NULL DEFAULT '',
    [CreatedAt]   DATETIMEOFFSET   NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ExpiresAt]   DATETIMEOFFSET   NOT NULL,
    [ConsumedAt]  DATETIMEOFFSET   NULL,
    [RevokedAt]   DATETIMEOFFSET   NULL,
    CONSTRAINT [PK_EnrollmentTokens] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_EnrollmentTokens_ExpiresAt] ON [dbo].[EnrollmentTokens] ([ExpiresAt]);
