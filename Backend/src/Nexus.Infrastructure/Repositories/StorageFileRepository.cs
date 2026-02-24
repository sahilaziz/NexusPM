using Microsoft.EntityFrameworkCore;
using Nexus.Application.Services;
using Nexus.Infrastructure.Data;

namespace Nexus.Infrastructure.Repositories;

public class StorageFileRepository : IStorageFileRepository
{
    private readonly AppDbContext _context;

    public StorageFileRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<long> CreateAsync(StoredFileInfo file)
    {
        var entity = new StoredFileEntity
        {
            DocumentId = file.DocumentId,
            StorageId = file.StorageId,
            OriginalFileName = file.OriginalFileName,
            StoragePath = file.StoragePath,
            ExternalFileId = file.ExternalFileId,
            PublicUrl = file.PublicUrl,
            FileSize = file.FileSize,
            MimeType = file.MimeType,
            Checksum = file.Checksum,
            UploadedAt = file.UploadedAt,
            UploadedBy = file.UploadedBy,
            IsDeleted = false
        };

        _context.StoredFiles.Add(entity);
        await _context.SaveChangesAsync();

        return entity.FileId;
    }

    public async Task<StoredFileInfo?> GetByIdAsync(long fileId)
    {
        var entity = await _context.StoredFiles
            .FirstOrDefaultAsync(f => f.FileId == fileId);

        return entity == null ? null : MapToDomain(entity);
    }

    public async Task<IEnumerable<StoredFileInfo>> GetByDocumentIdAsync(long documentId)
    {
        var entities = await _context.StoredFiles
            .Where(f => f.DocumentId == documentId && !f.IsDeleted)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync();

        return entities.Select(MapToDomain);
    }

    public async Task MarkAsDeletedAsync(long fileId, string deletedBy)
    {
        var entity = await _context.StoredFiles.FindAsync(fileId);
        if (entity != null)
        {
            entity.IsDeleted = true;
            // Silen istifadəçini log etmək üçün əlavə field əlavə edə bilərsiniz
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdatePublicUrlAsync(long fileId, string publicUrl)
    {
        var entity = await _context.StoredFiles.FindAsync(fileId);
        if (entity != null)
        {
            entity.PublicUrl = publicUrl;
            await _context.SaveChangesAsync();
        }
    }

    private static StoredFileInfo MapToDomain(StoredFileEntity entity)
    {
        return new StoredFileInfo
        {
            FileId = entity.FileId,
            DocumentId = entity.DocumentId,
            StorageId = entity.StorageId,
            OriginalFileName = entity.OriginalFileName,
            StoragePath = entity.StoragePath,
            ExternalFileId = entity.ExternalFileId,
            PublicUrl = entity.PublicUrl,
            FileSize = entity.FileSize,
            MimeType = entity.MimeType,
            Checksum = entity.Checksum,
            UploadedAt = entity.UploadedAt,
            UploadedBy = entity.UploadedBy,
            IsDeleted = entity.IsDeleted
        };
    }
}
