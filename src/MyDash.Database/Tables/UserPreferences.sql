CREATE TABLE [dbo].[UserPreferences] (
    [Id]                   INT           NOT NULL DEFAULT 1,
    [Theme]                NVARCHAR(16)  NOT NULL DEFAULT 'Dark',
    [Accent]               NVARCHAR(16)  NOT NULL DEFAULT '#594AE2',
    [DefaultServiceView]   NVARCHAR(16)  NOT NULL DEFAULT 'cards',
    [Density]              NVARCHAR(16)  NOT NULL DEFAULT 'normal',
    [NotifyOnNewPorts]     BIT           NOT NULL DEFAULT 0,
    [NotifyOnDisconnect]   BIT           NOT NULL DEFAULT 0,
    [ScanSchedule]         NVARCHAR(64)  NULL,
    [PhoneE164]            NVARCHAR(32)  NULL,
    CONSTRAINT [PK_UserPreferences]          PRIMARY KEY ([Id]),
    CONSTRAINT [CHK_UserPreferences_Single]  CHECK ([Id] = 1)
);
