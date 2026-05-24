CREATE TABLE [dbo].[AuditEntries] (
    [Id]       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [At]       DATETIMEOFFSET   NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [Actor]    NVARCHAR(128)    NOT NULL DEFAULT 'system',
    [Action]   INT              NOT NULL,
    [Target]   NVARCHAR(512)    NOT NULL DEFAULT '',
    [Ip]       NVARCHAR(64)     NOT NULL DEFAULT '',
    [Outcome]  NVARCHAR(64)     NOT NULL DEFAULT 'ok',
    [Metadata] NVARCHAR(MAX)    NULL,
    CONSTRAINT [PK_AuditEntries] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_AuditEntries_At] ON [dbo].[AuditEntries] ([At] DESC);
