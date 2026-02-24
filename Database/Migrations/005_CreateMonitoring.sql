-- Migration: Create Monitoring Tables
-- Tam şəxsi monitoring sistemi

-- System Logs table
CREATE TABLE [dbo].[SystemLogs] (
    [LogId] BIGINT IDENTITY(1,1) PRIMARY KEY,
    [Level] INT NOT NULL DEFAULT 1, -- 0:Debug, 1:Info, 2:Warning, 3:Error, 4:Critical
    [Category] NVARCHAR(100) NOT NULL,
    [Message] NVARCHAR(MAX) NOT NULL,
    [Details] NVARCHAR(MAX) NULL,
    [Exception] NVARCHAR(MAX) NULL,
    [Endpoint] NVARCHAR(500) NULL,
    [HttpMethod] NVARCHAR(10) NULL,
    [StatusCode] INT NULL,
    [DurationMs] BIGINT NULL,
    [ClientIp] NVARCHAR(50) NULL,
    [UserId] NVARCHAR(100) NULL,
    [UserAgent] NVARCHAR(500) NULL,
    [OrganizationCode] NVARCHAR(50) NOT NULL DEFAULT 'default',
    [CorrelationId] NVARCHAR(100) NULL,
    [MachineName] NVARCHAR(100) NULL,
    [Timestamp] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Indexes
CREATE INDEX [IX_SystemLogs_Timestamp] ON [dbo].[SystemLogs] ([Timestamp] DESC);
CREATE INDEX [IX_SystemLogs_Level] ON [dbo].[SystemLogs] ([Level]);
CREATE INDEX [IX_SystemLogs_Category] ON [dbo].[SystemLogs] ([Category]);
CREATE INDEX [IX_SystemLogs_Endpoint] ON [dbo].[SystemLogs] ([Endpoint]);
CREATE INDEX [IX_SystemLogs_UserId] ON [dbo].[SystemLogs] ([UserId]);
CREATE INDEX [IX_SystemLogs_Timestamp_Level] ON [dbo].[SystemLogs] ([Timestamp], [Level]);

-- Performance Metrics table
CREATE TABLE [dbo].[PerformanceMetrics] (
    [MetricId] BIGINT IDENTITY(1,1) PRIMARY KEY,
    [MetricName] NVARCHAR(100) NOT NULL,
    [Value] FLOAT NOT NULL,
    [Unit] NVARCHAR(20) NULL,
    [Tags] NVARCHAR(MAX) NULL,
    [MachineName] NVARCHAR(100) NULL,
    [Timestamp] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

-- Indexes
CREATE INDEX [IX_PerformanceMetrics_MetricName_Timestamp] 
    ON [dbo].[PerformanceMetrics] ([MetricName], [Timestamp] DESC);
CREATE INDEX [IX_PerformanceMetrics_Timestamp] 
    ON [dbo].[PerformanceMetrics] ([Timestamp] DESC);

-- Monitoring Config table
CREATE TABLE [dbo].[MonitoringConfigs] (
    [ConfigId] BIGINT PRIMARY KEY DEFAULT 1,
    [IsEnabled] BIT NOT NULL DEFAULT 1,
    [LogRequests] BIT NOT NULL DEFAULT 1,
    [LogErrors] BIT NOT NULL DEFAULT 1,
    [TrackPerformance] BIT NOT NULL DEFAULT 1,
    [LogDatabaseQueries] BIT NOT NULL DEFAULT 0,
    [MinimumLogLevel] INT NOT NULL DEFAULT 1,
    [RetentionDays] INT NOT NULL DEFAULT 30,
    [SlowRequestThresholdMs] INT NOT NULL DEFAULT 1000,
    [AlertEmail] NVARCHAR(200) NULL,
    [CpuAlertThreshold] INT NOT NULL DEFAULT 80,
    [MemoryAlertThreshold] INT NOT NULL DEFAULT 85,
    [ErrorRateAlertThreshold] INT NOT NULL DEFAULT 5,
    [ModifiedAt] DATETIME2 NULL,
    [ModifiedBy] NVARCHAR(100) NULL
);

-- Default config insert
INSERT INTO [dbo].[MonitoringConfigs] (
    [ConfigId], [IsEnabled], [LogRequests], [LogErrors], [TrackPerformance],
    [LogDatabaseQueries], [MinimumLogLevel], [RetentionDays], 
    [SlowRequestThresholdMs], [AlertEmail], [CpuAlertThreshold], 
    [MemoryAlertThreshold], [ErrorRateAlertThreshold]
)
VALUES (
    1, 1, 1, 1, 1, 0, 1, 30, 1000, NULL, 80, 85, 5
);

-- Cleanup procedure
CREATE OR ALTER PROCEDURE [dbo].[sp_CleanupOldMonitoringData]
    @RetentionDays INT = 30
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -@RetentionDays, GETUTCDATE());
    
    -- Old logs
    DELETE FROM [dbo].[SystemLogs] WHERE [Timestamp] < @CutoffDate;
    
    -- Old metrics
    DELETE FROM [dbo].[PerformanceMetrics] WHERE [Timestamp] < @CutoffDate;
    
    SELECT @@ROWCOUNT AS DeletedRows;
END;
GO

-- Dashboard view
CREATE OR ALTER VIEW [dbo].[vw_MonitoringDashboard] AS
SELECT 
    (SELECT COUNT(*) FROM [dbo].[SystemLogs] 
     WHERE [Timestamp] > DATEADD(HOUR, -1, GETUTCDATE()) 
     AND [Category] = 'Request') AS RequestsLastHour,
    
    (SELECT COUNT(*) FROM [dbo].[SystemLogs] 
     WHERE [Timestamp] > DATEADD(HOUR, -1, GETUTCDATE()) 
     AND [Level] >= 3) AS ErrorsLastHour,
    
    (SELECT AVG([DurationMs]) FROM [dbo].[SystemLogs] 
     WHERE [Timestamp] > DATEADD(HOUR, -1, GETUTCDATE()) 
     AND [Category] = 'Request' 
     AND [DurationMs] IS NOT NULL) AS AvgResponseTimeLastHour,
    
    (SELECT TOP 1 [Value] FROM [dbo].[PerformanceMetrics] 
     WHERE [MetricName] = 'CpuUsage' 
     ORDER BY [Timestamp] DESC) AS CurrentCpuUsage,
    
    (SELECT TOP 1 [Value] FROM [dbo].[PerformanceMetrics] 
     WHERE [MetricName] = 'MemoryUsage' 
     ORDER BY [Timestamp] DESC) AS CurrentMemoryUsage;
GO

PRINT 'Monitoring tables created successfully.';
PRINT 'Default config: Monitoring ENABLED';
