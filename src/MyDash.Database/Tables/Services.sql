CREATE TABLE [dbo].[Services] (
    [Id]                  UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [ServerId]            UNIQUEIDENTIFIER NOT NULL,
    [Name]                NVARCHAR(128)    NOT NULL,
    [Port]                INT              NOT NULL,
    [Type]                INT              NOT NULL DEFAULT 1,
    [DockerImage]         NVARCHAR(256)    NULL,
    [DockerContainerId]   NVARCHAR(64)     NULL,
    [Tags]                NVARCHAR(1024)   NOT NULL DEFAULT '',
    [Description]         NVARCHAR(512)    NOT NULL DEFAULT '',
    [HealthEndpoint]      NVARCHAR(512)    NULL,
    [IconColor]           NVARCHAR(32)     NOT NULL DEFAULT '#594AE2',
    [IconGlyph]           NVARCHAR(64)     NOT NULL DEFAULT 'router',
    [LastCheck]           DATETIMEOFFSET   NULL,
    [Status]              INT              NOT NULL DEFAULT 3,
    [IsPinned]            BIT              NOT NULL DEFAULT 0,
    CONSTRAINT [PK_Services]          PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Services_Servers]  FOREIGN KEY ([ServerId])
        REFERENCES [dbo].[Servers] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Services_ServerId] ON [dbo].[Services] ([ServerId]);
