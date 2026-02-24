-- Migration: Create Message Queue Tables
-- Tam şəxsi Event Bus üçün

-- Message Queue table
CREATE TABLE [dbo].[MessageQueues] (
    [MessageId] BIGINT IDENTITY(1,1) PRIMARY KEY,
    [QueueName] NVARCHAR(100) NOT NULL,
    [MessageType] NVARCHAR(200) NOT NULL,
    [Payload] NVARCHAR(MAX) NOT NULL,
    [Status] INT NOT NULL DEFAULT 0, -- 0: Pending, 1: Processing, 2: Completed, 3: Failed, 4: DeadLetter
    [RetryCount] INT NOT NULL DEFAULT 0,
    [MaxRetries] INT NOT NULL DEFAULT 3,
    [ScheduledFor] DATETIME2 NULL,
    [ErrorMessage] NVARCHAR(4000) NULL,
    [Priority] INT NOT NULL DEFAULT 0,
    [CorrelationId] NVARCHAR(100) NOT NULL,
    [OrganizationCode] NVARCHAR(50) NOT NULL DEFAULT 'default',
    [CreatedBy] NVARCHAR(100) NOT NULL DEFAULT 'system',
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [ProcessedAt] DATETIME2 NULL,
    [ExpiresAt] DATETIME2 NULL
);

-- Indexes
CREATE INDEX [IX_MessageQueues_QueueName_Status] ON [dbo].[MessageQueues] ([QueueName], [Status]);
CREATE INDEX [IX_MessageQueues_Status_ScheduledFor] ON [dbo].[MessageQueues] ([Status], [ScheduledFor]);
CREATE INDEX [IX_MessageQueues_CreatedAt] ON [dbo].[MessageQueues] ([CreatedAt]);
CREATE INDEX [IX_MessageQueues_CorrelationId] ON [dbo].[MessageQueues] ([CorrelationId]);
CREATE INDEX [IX_MessageQueues_Priority_CreatedAt] ON [dbo].[MessageQueues] ([Priority] DESC, [CreatedAt] ASC);

-- Dead Letter Queue table
CREATE TABLE [dbo].[DeadLetterMessages] (
    [DeadLetterId] BIGINT IDENTITY(1,1) PRIMARY KEY,
    [OriginalMessageId] BIGINT NOT NULL,
    [QueueName] NVARCHAR(100) NOT NULL,
    [MessageType] NVARCHAR(200) NOT NULL,
    [Payload] NVARCHAR(MAX) NOT NULL,
    [RetryCount] INT NOT NULL,
    [ErrorMessage] NVARCHAR(4000) NULL,
    [FailedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [OrganizationCode] NVARCHAR(50) NOT NULL DEFAULT 'default'
);

CREATE INDEX [IX_DeadLetterMessages_QueueName] ON [dbo].[DeadLetterMessages] ([QueueName]);
CREATE INDEX [IX_DeadLetterMessages_FailedAt] ON [dbo].[DeadLetterMessages] ([FailedAt]);

-- Cleanup procedure
CREATE OR ALTER PROCEDURE [dbo].[sp_CleanupOldMessages]
    @RetentionDays INT = 7
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -@RetentionDays, GETUTCDATE());
    
    -- Silinənlərin sayını qeyd et
    DECLARE @DeletedCount INT;
    
    SELECT @DeletedCount = COUNT(*) FROM [dbo].[MessageQueues]
    WHERE [Status] = 2 -- Completed
        AND [ProcessedAt] < @CutoffDate;
    
    -- Köhnə message-ləri sil
    DELETE FROM [dbo].[MessageQueues]
    WHERE [Status] = 2 -- Completed
        AND [ProcessedAt] < @CutoffDate;
    
    SELECT @DeletedCount AS DeletedCount;
END;
GO

-- Stuck messages reset procedure
CREATE OR ALTER PROCEDURE [dbo].[sp_ResetStuckMessages]
    @TimeoutMinutes INT = 15
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @StuckTime DATETIME2 = DATEADD(MINUTE, -@TimeoutMinutes, GETUTCDATE());
    
    UPDATE [dbo].[MessageQueues]
    SET [Status] = 0, -- Pending
        [ProcessedAt] = NULL,
        [ErrorMessage] = 'Reset due to timeout'
    WHERE [Status] = 1 -- Processing
        AND [ProcessedAt] < @StuckTime;
    
    SELECT @@ROWCOUNT AS ResetCount;
END;
GO

-- Queue status view
CREATE OR ALTER VIEW [dbo].[vw_QueueStatus] AS
SELECT 
    [QueueName],
    [Status],
    COUNT(*) AS MessageCount,
    MIN([CreatedAt]) AS OldestMessage,
    MAX([CreatedAt]) AS NewestMessage
FROM [dbo].[MessageQueues]
GROUP BY [QueueName], [Status];
GO

PRINT 'Message Queue tables created successfully.';
