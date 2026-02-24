-- Nexus Project Management - Database Schema
-- SQL Server 2022

CREATE DATABASE NexusDB
    COLLATE Cyrillic_General_CI_AS; -- Azərbaycan/Rus dil dəstəyi
GO

USE NexusDB;
GO

-- =============================================
-- 1. HIERARCHICAL DOCUMENT MANAGEMENT
-- =============================================

-- Əsas qovluq/sənəd node cədvəli
CREATE TABLE DocumentNodes (
    NodeId BIGINT PRIMARY KEY IDENTITY(1,1),
    ParentNodeId BIGINT NULL FOREIGN KEY REFERENCES DocumentNodes(NodeId),
    NodeType VARCHAR(20) CHECK (NodeType IN ('ROOT', 'IDARE', 'QUYU', 'MENTEQE', 'DOCUMENT')),
    
    -- Unikal identifikatorlar
    EntityCode VARCHAR(100) NOT NULL, -- "AZNEFT_IB", "20_SAYLI_QUYU"
    EntityName NVARCHAR(500) NOT NULL, -- "Azneft İB", "20 saylı quyu"
    
    -- Hiyerarşik yol (performans üçün)
    MaterializedPath VARCHAR(1000), -- /1/5/12/25/
    Depth INT DEFAULT 0,
    
    -- Sənəd metadata
    DocumentDate DATE NULL,
    DocumentNumber VARCHAR(100) NULL,           -- Original: 1-4-8\3-2-1243\2026
    NormalizedDocumentNumber VARCHAR(200) NULL, -- Search: 1 4 8 3 2 1243 2026
    DocumentSubject NVARCHAR(2000) NULL,
    ExternalDocumentNumber VARCHAR(100) NULL,   -- Xarici sənəd nömrəsi
    SourceType VARCHAR(20) DEFAULT 'INCOMING_LETTER' 
        CHECK (SourceType IN ('INCOMING_LETTER', 'INTERNAL_PROJECT', 'EXTERNAL_DOCUMENT')),
    
    -- Fayl sistemi yolu
    FileSystemPath NVARCHAR(1000),
    
    -- Workflow status
    Status VARCHAR(20) DEFAULT 'ACTIVE' 
        CHECK (Status IN ('ACTIVE', 'ARCHIVED', 'DELETED')),
    
    -- Audit
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy VARCHAR(100) DEFAULT SYSTEM_USER,
    ModifiedAt DATETIME2 NULL,
    ModifiedBy VARCHAR(100) NULL,
    
    -- Constraint: Eyni parent altında eyni ad olmaz!
    CONSTRAINT UQ_Node UNIQUE (ParentNodeId, EntityCode)
);

-- Hiyerarşik əlaqələr cədvəli (Closure Table Pattern)
CREATE TABLE NodePaths (
    AncestorId BIGINT FOREIGN KEY REFERENCES DocumentNodes(NodeId),
    DescendantId BIGINT FOREIGN KEY REFERENCES DocumentNodes(NodeId),
    Depth INT, -- 0 = özü, 1 = birbaşa uşaq, 2 = nəvə...
    PRIMARY KEY (AncestorId, DescendantId)
);

-- Root node yaradılması
INSERT INTO DocumentNodes (NodeId, NodeType, EntityCode, EntityName, MaterializedPath, Depth)
VALUES (1, 'ROOT', 'ROOT', 'Root', '/1/', 0);

SET IDENTITY_INSERT DocumentNodes OFF;

-- Index-lər
CREATE INDEX IX_NodePaths_Descendant ON NodePaths(DescendantId);
CREATE INDEX IX_DocumentNodes_EntityCode ON DocumentNodes(EntityCode);
CREATE INDEX IX_DocumentNodes_Parent ON DocumentNodes(ParentNodeId);
CREATE INDEX IX_DocumentNodes_Date ON DocumentNodes(DocumentDate) WHERE NodeType = 'DOCUMENT';
CREATE INDEX IX_DocumentNodes_Normalized ON DocumentNodes(NormalizedDocumentNumber);
CREATE INDEX IX_DocumentNodes_Source ON DocumentNodes(SourceType);
CREATE INDEX IX_DocumentNodes_Status ON DocumentNodes(Status);

-- =============================================
-- 2. PROJECT MANAGEMENT
-- =============================================

CREATE TABLE Projects (
    ProjectId BIGINT PRIMARY KEY IDENTITY(1,1),
    ProjectCode VARCHAR(50) UNIQUE NOT NULL,
    ProjectName NVARCHAR(500) NOT NULL,
    Description NVARCHAR(MAX),
    
    -- Əlaqəli qovluq (sənədlər üçün)
    DocumentNodeId BIGINT FOREIGN KEY REFERENCES DocumentNodes(NodeId),
    
    -- Status və tarixlər
    Status VARCHAR(20) DEFAULT 'ACTIVE' 
        CHECK (Status IN ('PLANNING', 'ACTIVE', 'ON_HOLD', 'COMPLETED', 'CANCELLED')),
    StartDate DATE,
    EndDate DATE,
    
    -- Audit
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy VARCHAR(100),
    ModifiedAt DATETIME2,
    ModifiedBy VARCHAR(100)
);

CREATE TABLE Tasks (
    TaskId BIGINT PRIMARY KEY IDENTITY(1,1),
    ProjectId BIGINT FOREIGN KEY REFERENCES Projects(ProjectId),
    ParentTaskId BIGINT NULL FOREIGN KEY REFERENCES Tasks(TaskId),
    
    TaskTitle NVARCHAR(500) NOT NULL,
    TaskDescription NVARCHAR(MAX),
    
    -- Assignee
    AssignedTo VARCHAR(100),
    CreatedBy VARCHAR(100),
    
    -- Status və prioritet
    Status VARCHAR(20) DEFAULT 'TODO' 
        CHECK (Status IN ('TODO', 'IN_PROGRESS', 'REVIEW', 'DONE', 'CANCELLED')),
    Priority VARCHAR(10) DEFAULT 'MEDIUM' 
        CHECK (Priority IN ('LOW', 'MEDIUM', 'HIGH', 'CRITICAL')),
    
    -- Tarixlər
    DueDate DATETIME2,
    CompletedAt DATETIME2,
    
    -- Əlaqəli sənəd
    DocumentNodeId BIGINT FOREIGN KEY REFERENCES DocumentNodes(NodeId),
    
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    ModifiedAt DATETIME2
);

-- Task Comments
CREATE TABLE TaskComments (
    CommentId BIGINT PRIMARY KEY IDENTITY(1,1),
    TaskId BIGINT FOREIGN KEY REFERENCES Tasks(TaskId),
    UserId VARCHAR(100),
    Content NVARCHAR(MAX),
    CreatedAt DATETIME2 DEFAULT GETDATE()
);

-- Task Attachments
CREATE TABLE TaskAttachments (
    AttachmentId BIGINT PRIMARY KEY IDENTITY(1,1),
    TaskId BIGINT FOREIGN KEY REFERENCES Tasks(TaskId),
    FileName NVARCHAR(500),
    FilePath NVARCHAR(1000),
    FileSize BIGINT,
    UploadedAt DATETIME2 DEFAULT GETDATE(),
    UploadedBy VARCHAR(100)
);

-- =============================================
-- 3. USERS & PERMISSIONS
-- =============================================

CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(100) NOT NULL UNIQUE,
    Email VARCHAR(255) UNIQUE,
    FullName NVARCHAR(200) NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Role VARCHAR(20) DEFAULT 'USER' CHECK (Role IN ('ADMIN', 'MANAGER', 'USER')),
    OrganizationCode VARCHAR(50) DEFAULT 'AZNEFT_IB',
    Department NVARCHAR(100),
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    ModifiedAt DATETIME2,
    LastLoginAt DATETIME2
);

CREATE TABLE UserProjectRoles (
    UserId INT FOREIGN KEY REFERENCES Users(UserId),
    ProjectId BIGINT FOREIGN KEY REFERENCES Projects(ProjectId),
    Role VARCHAR(20) CHECK (Role IN ('OWNER', 'ADMIN', 'MEMBER', 'VIEWER')),
    AssignedAt DATETIME2 DEFAULT GETDATE(),
    PRIMARY KEY (UserId, ProjectId)
);

CREATE INDEX IX_Users_Organization ON Users(OrganizationCode);
CREATE INDEX IX_Users_Username ON Users(Username);

-- =============================================
-- 4. STORAGE SYSTEM
-- =============================================

CREATE TABLE StorageSettings (
    StorageId INT PRIMARY KEY IDENTITY(1,1),
    StorageName NVARCHAR(200) NOT NULL,
    StorageType VARCHAR(20) CHECK (StorageType IN ('LOCALDISK', 'FTPSERVER', 'ONEDRIVE', 'GOOGLEDRIVE', 'NETWORKSHARE')),
    IsDefault BIT DEFAULT 0,
    IsActive BIT DEFAULT 1,
    ConfigurationJson NVARCHAR(MAX) NOT NULL DEFAULT '{}',  -- JSON config
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    CreatedBy VARCHAR(100)
);

CREATE INDEX IX_StorageSettings_Default ON StorageSettings(IsDefault) WHERE IsDefault = 1;
CREATE INDEX IX_StorageSettings_Active ON StorageSettings(IsActive, StorageType);

CREATE TABLE StoredFiles (
    FileId BIGINT PRIMARY KEY IDENTITY(1,1),
    DocumentId BIGINT NOT NULL FOREIGN KEY REFERENCES DocumentNodes(NodeId),
    StorageId INT NOT NULL FOREIGN KEY REFERENCES StorageSettings(StorageId),
    OriginalFileName NVARCHAR(500) NOT NULL,
    StoragePath NVARCHAR(1000) NOT NULL,        -- Storage-dəki yol
    ExternalFileId VARCHAR(200) NULL,           -- Cloud üçün (OneDrive file ID)
    PublicUrl NVARCHAR(1000) NULL,              -- Birbaşa URL
    FileSize BIGINT NOT NULL,
    MimeType VARCHAR(100) NOT NULL,
    Checksum VARCHAR(64) NULL,                  -- MD5 hash
    UploadedAt DATETIME2 DEFAULT GETDATE(),
    UploadedBy VARCHAR(100),
    IsDeleted BIT DEFAULT 0
);

CREATE INDEX IX_StoredFiles_Document ON StoredFiles(DocumentId) WHERE IsDeleted = 0;
CREATE INDEX IX_StoredFiles_Storage ON StoredFiles(StorageId);

-- =============================================
-- 5. SYNC QUEUE (Offline-First)
-- =============================================

CREATE TABLE SyncQueue (
    QueueId BIGINT PRIMARY KEY IDENTITY(1,1),
    DeviceId VARCHAR(100),
    OrganizationCode VARCHAR(50) DEFAULT 'AZNEFT_IB',
    Operation VARCHAR(20) CHECK (Operation IN ('CREATE', 'UPDATE', 'DELETE')),
    EntityType VARCHAR(50), -- 'Task', 'Project', 'Document'
    EntityId BIGINT,
    Payload NVARCHAR(MAX), -- JSON
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    Processed BIT DEFAULT 0,
    ProcessedAt DATETIME2,
    ErrorMessage NVARCHAR(MAX)
);

CREATE INDEX IX_SyncQueue_Device ON SyncQueue(DeviceId, Processed);
CREATE INDEX IX_SyncQueue_Created ON SyncQueue(CreatedAt) WHERE Processed = 0;

PRINT 'Nexus Database Schema created successfully!';
