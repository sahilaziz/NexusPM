-- Nexus Project Management - Stored Procedures
-- Smart Foldering System

USE NexusDB;
GO

-- =============================================
-- Smart Folder Creation
-- =============================================
CREATE PROCEDURE sp_GetOrCreatePath
    @IdareCode VARCHAR(100),      -- "AZNEFT"
    @IdareName NVARCHAR(500),     -- "Azneft İB"
    @QuyuCode VARCHAR(100),       -- "QUYU_20"
    @QuyuName NVARCHAR(500),      -- "20 saylı quyu"
    @MenteqeCode VARCHAR(100),    -- "YASAYIS_20"
    @MenteqeName NVARCHAR(500),   -- "Yaşayış məntəqəsi"
    @DocumentDate DATE,
    @DocNumber VARCHAR(100),
    @DocSubject NVARCHAR(2000),
    @OpenTextId VARCHAR(100),
    @CreatedBy VARCHAR(100) = SYSTEM_USER
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @RootId BIGINT = 1;
    DECLARE @IdareId BIGINT, @QuyuId BIGINT, @MenteqeId BIGINT, @DocId BIGINT;
    DECLARE @BasePath NVARCHAR(500) = '\Server\Nexus\Documents';
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- 1. İDARƏ YOXLA/YARAT
        SELECT @IdareId = NodeId 
        FROM DocumentNodes 
        WHERE ParentNodeId = @RootId AND EntityCode = @IdareCode;
        
        IF @IdareId IS NULL
        BEGIN
            INSERT INTO DocumentNodes (
                ParentNodeId, NodeType, EntityCode, EntityName, 
                Depth, MaterializedPath, FileSystemPath, CreatedBy
            )
            VALUES (
                @RootId, 'IDARE', @IdareCode, @IdareName, 
                1, '/1/', @BasePath + '\' + @IdareCode, @CreatedBy
            );
            
            SET @IdareId = SCOPE_IDENTITY();
            
            -- MaterializedPath update
            UPDATE DocumentNodes 
            SET MaterializedPath = '/1/' + CAST(@IdareId AS VARCHAR) + '/'
            WHERE NodeId = @IdareId;
            
            -- Closure Table
            INSERT INTO NodePaths (AncestorId, DescendantId, Depth)
            VALUES (@RootId, @IdareId, 1), (@IdareId, @IdareId, 0);
        END
        
        -- 2. QUYU YOXLA/YARAT
        SELECT @QuyuId = NodeId 
        FROM DocumentNodes 
        WHERE ParentNodeId = @IdareId AND EntityCode = @QuyuCode;
        
        IF @QuyuId IS NULL
        BEGIN
            INSERT INTO DocumentNodes (
                ParentNodeId, NodeType, EntityCode, EntityName,
                Depth, FileSystemPath, CreatedBy
            )
            VALUES (
                @IdareId, 'QUYU', @QuyuCode, @QuyuName,
                2, @BasePath + '\' + @IdareCode + '\' + @QuyuCode, @CreatedBy
            );
            
            SET @QuyuId = SCOPE_IDENTITY();
            
            UPDATE DocumentNodes 
            SET MaterializedPath = (SELECT MaterializedPath FROM DocumentNodes WHERE NodeId = @IdareId) + CAST(@QuyuId AS VARCHAR) + '/'
            WHERE NodeId = @QuyuId;
            
            -- Closure Table
            INSERT INTO NodePaths (AncestorId, DescendantId, Depth)
            SELECT AncestorId, @QuyuId, Depth + 1 FROM NodePaths WHERE DescendantId = @IdareId
            UNION ALL SELECT @QuyuId, @QuyuId, 0;
        END
        
        -- 3. YAŞAYIŞ MƏNTƏQƏSİ YOXLA/YARAT
        SELECT @MenteqeId = NodeId 
        FROM DocumentNodes 
        WHERE ParentNodeId = @QuyuId AND EntityCode = @MenteqeCode;
        
        IF @MenteqeId IS NULL
        BEGIN
            INSERT INTO DocumentNodes (
                ParentNodeId, NodeType, EntityCode, EntityName,
                Depth, FileSystemPath, CreatedBy
            )
            VALUES (
                @QuyuId, 'MENTEQE', @MenteqeCode, @MenteqeName,
                3, @BasePath + '\' + @IdareCode + '\' + @QuyuCode + '\' + @MenteqeCode, @CreatedBy
            );
            
            SET @MenteqeId = SCOPE_IDENTITY();
            
            UPDATE DocumentNodes 
            SET MaterializedPath = (SELECT MaterializedPath FROM DocumentNodes WHERE NodeId = @QuyuId) + CAST(@MenteqeId AS VARCHAR) + '/'
            WHERE NodeId = @MenteqeId;
            
            -- Closure Table
            INSERT INTO NodePaths (AncestorId, DescendantId, Depth)
            SELECT AncestorId, @MenteqeId, Depth + 1 FROM NodePaths WHERE DescendantId = @QuyuId
            UNION ALL SELECT @MenteqeId, @MenteqeId, 0;
        END
        
        -- 4. SƏNƏD YARAT
        DECLARE @FileName NVARCHAR(500) = 
            FORMAT(@DocumentDate, 'yyyy-MM-dd') + ' - Məktub №' + @DocNumber + ' - ' + 
            LEFT(@DocSubject, 30) + CASE WHEN LEN(@DocSubject) > 30 THEN '...' ELSE '' END;
        
        -- Fayl adından illegal character-ləri təmizlə
        SET @FileName = REPLACE(REPLACE(REPLACE(@FileName, '\', '_'), '/', '_'), ':', '_');
        
        INSERT INTO DocumentNodes (
            ParentNodeId, NodeType, EntityCode, EntityName,
            DocumentDate, DocumentNumber, DocumentSubject, OpenTextId,
            Depth, FileSystemPath, CreatedBy
        )
        VALUES (
            @MenteqeId, 'DOCUMENT', @DocNumber, @FileName,
            @DocumentDate, @DocNumber, @DocSubject, @OpenTextId,
            4, @BasePath + '\' + @IdareCode + '\' + @QuyuCode + '\' + @MenteqeCode + '\' + @FileName + '.pdf',
            @CreatedBy
        );
        
        SET @DocId = SCOPE_IDENTITY();
        
        COMMIT TRANSACTION;
        
        -- Return result
        SELECT 
            @DocId AS DocumentId, 
            @MenteqeId AS ParentFolderId,
            @FileName AS GeneratedFileName,
            'SUCCESS' AS Status;
            
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
            
        SELECT 
            0 AS DocumentId,
            0 AS ParentFolderId,
            ERROR_MESSAGE() AS ErrorMessage,
            'ERROR' AS Status;
    END CATCH
END
GO

-- =============================================
-- Get Folder Tree
-- =============================================
CREATE PROCEDURE sp_GetFolderTree
    @ParentNodeId BIGINT = 1
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        n.NodeId,
        n.ParentNodeId,
        n.NodeType,
        n.EntityCode,
        n.EntityName,
        n.Depth,
        n.DocumentDate,
        n.DocumentNumber,
        n.Status,
        n.CreatedAt,
        -- Child count
        (SELECT COUNT(*) FROM DocumentNodes WHERE ParentNodeId = n.NodeId) AS ChildCount
    FROM DocumentNodes n
    WHERE n.ParentNodeId = @ParentNodeId
      AND n.Status = 'ACTIVE'
    ORDER BY 
        CASE n.NodeType 
            WHEN 'IDARE' THEN 1
            WHEN 'QUYU' THEN 2
            WHEN 'MENTEQE' THEN 3
            WHEN 'DOCUMENT' THEN 4
        END,
        n.EntityName;
END
GO

-- =============================================
-- Search Documents
-- =============================================
CREATE PROCEDURE sp_SearchDocuments
    @SearchTerm NVARCHAR(500),
    @IdareCode VARCHAR(100) = NULL,
    @DateFrom DATE = NULL,
    @DateTo DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        n.NodeId,
        n.EntityName,
        n.DocumentNumber,
        n.DocumentDate,
        n.DocumentSubject,
        n.FileSystemPath,
        n.CreatedAt,
        -- Full path
        STRING_AGG(p.EntityName, ' > ') WITHIN GROUP (ORDER BY np.Depth DESC) AS FullPath
    FROM DocumentNodes n
    JOIN NodePaths np ON n.NodeId = np.DescendantId
    JOIN DocumentNodes p ON np.AncestorId = p.NodeId
    WHERE n.NodeType = 'DOCUMENT'
      AND n.Status = 'ACTIVE'
      AND (
          n.EntityName LIKE '%' + @SearchTerm + '%'
          OR n.DocumentNumber LIKE '%' + @SearchTerm + '%'
          OR n.DocumentSubject LIKE '%' + @SearchTerm + '%'
      )
      AND (@IdareCode IS NULL OR EXISTS (
          SELECT 1 FROM NodePaths np2 
          JOIN DocumentNodes n2 ON np2.AncestorId = n2.NodeId
          WHERE np2.DescendantId = n.NodeId AND n2.EntityCode = @IdareCode
      ))
      AND (@DateFrom IS NULL OR n.DocumentDate >= @DateFrom)
      AND (@DateTo IS NULL OR n.DocumentDate <= @DateTo)
    GROUP BY n.NodeId, n.EntityName, n.DocumentNumber, n.DocumentDate, n.DocumentSubject, n.FileSystemPath, n.CreatedAt
    ORDER BY n.DocumentDate DESC;
END
GO

PRINT 'Stored Procedures created successfully!';
