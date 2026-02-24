-- Migration: Update Users table for Active Directory and Recovery Email support
-- Created: 2026-02-24

-- Add new columns to Users table
ALTER TABLE Users ADD RecoveryEmail NVARCHAR(256) NULL;
ALTER TABLE Users ADD IsRecoveryEmailConfirmed BIT NOT NULL DEFAULT 0;
ALTER TABLE Users ADD RecoveryEmailConfirmationToken NVARCHAR(256) NULL;
ALTER TABLE Users ADD IsProfileCompleted BIT NOT NULL DEFAULT 0;

-- Add new AD-related columns
ALTER TABLE Users ADD Domain NVARCHAR(100) NULL;
ALTER TABLE Users ADD ActiveDirectorySid NVARCHAR(100) NULL;
ALTER TABLE Users ADD AdGroups NVARCHAR(MAX) NULL;
ALTER TABLE Users ADD AuthenticationType NVARCHAR(20) NOT NULL DEFAULT 'Local';

-- Add indexes
CREATE INDEX IX_Users_RecoveryEmail ON Users(RecoveryEmail);
CREATE INDEX IX_Users_ActiveDirectorySid ON Users(ActiveDirectorySid);
CREATE INDEX IX_Users_AuthenticationType ON Users(AuthenticationType);
CREATE INDEX IX_Users_Username_AuthType ON Users(Username, AuthenticationType);

-- Update existing users to have Local authentication type
UPDATE Users SET AuthenticationType = 'Local' WHERE AuthenticationType IS NULL OR AuthenticationType = '';

PRINT 'Migration 003 completed successfully.';
