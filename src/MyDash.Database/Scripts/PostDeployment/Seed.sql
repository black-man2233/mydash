-- Ensure the singleton UserPreferences row always exists
IF NOT EXISTS (SELECT 1 FROM [dbo].[UserPreferences] WHERE [Id] = 1)
    INSERT INTO [dbo].[UserPreferences] ([Id]) VALUES (1);
