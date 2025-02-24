using Application.DTOs;
using Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Application.Interfaces;
public interface IFileService
{
    Task<FileDto> UploadFileAsync(IFormFile file, Guid userId, Guid? parentFolderId, Guid? fileEntryId = null);
    Task<FileDto> CreateFolderAsync(string name, Guid userId, Guid? parentFolderId);
    Task<FileDto> GetFileOrFolderAsync(Guid id, Guid userId);
    Task<FileDto> GetByShareLinkAsync(string shareLink, Guid userId);
    Task<ShareResponse> ShareFileOrFolderAsync(Guid id, Guid ownerId, Guid targetUserId, AccessLevel accessLevel);
    Task<List<FileDto>> ListFolderContentsAsync(Guid? folderId, Guid userId);
    Task<List<FileVersionDto>> GetFileVersionsAsync(Guid fileId, Guid userId);
    Task<FileDto> RestoreFileVersionAsync(Guid fileId, Guid userId, int versionNumber);
    Task DeleteFileOrFolderAsync(Guid id, Guid userId);
}
