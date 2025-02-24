using Domain.Entities;

namespace Domain.Interfaces;
public interface IFileRepository
{
    Task<FileEntry> GetByIdAsync(Guid id);
    Task<List<FileEntry>> GetChildrenAsync(Guid? parentFolderId, Guid ownerId);
    Task<List<FileEntry>> GetByOwnerIdAsync(Guid ownerId, Guid? parentFolderId);
    Task<FileEntry> GetByShareLinkAsync(string shareLink);
    Task AddAsync(FileEntry fileEntry);
    Task UpdateAsync(FileEntry fileEntry);
    Task DeleteAsync(Guid fileId);
    Task<List<FileVersion>> GetVersionsAsync(Guid fileEntryId);
}
