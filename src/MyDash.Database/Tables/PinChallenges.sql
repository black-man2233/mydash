CREATE TABLE [dbo].[PinChallenges] (
    [Id]             UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    [PhoneE164]      NVARCHAR(32)     NOT NULL,
    [CodeHash]       NVARCHAR(512)    NOT NULL,
    [IssuedAt]       DATETIMEOFFSET   NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ExpiresAt]      DATETIMEOFFSET   NOT NULL,
    [ConsumedAt]     DATETIMEOFFSET   NULL,
    [AttemptCount]   INT              NOT NULL DEFAULT 0,
    [FailedAttempts] INT              NOT NULL DEFAULT 0,
    CONSTRAINT [PK_PinChallenges] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_PinChallenges_ExpiresAt] ON [dbo].[PinChallenges] ([ExpiresAt]);
GO

CREATE INDEX [IX_PinChallenges_IssuedAt]  ON [dbo].[PinChallenges] ([IssuedAt]);
