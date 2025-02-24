using Domain.Entities;

namespace Domain.Interfaces;
public interface IFileVersionRepository
{
    Task<FileVersion> GetByIdAsync(Guid fileVersionId);
    Task<List<FileVersion>> GetByFileEntryIdAsync(Guid fileEntryId);
    Task<FileVersion> GetLatestVersionAsync(Guid fileEntryId);
    Task AddAsync(FileVersion fileVersion);
    Task UpdateAsync(FileVersion fileVersion);
    Task DeleteAsync(Guid fileVersionId);
}
