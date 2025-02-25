using Application.DTOs;
using Domain.Commons;
using Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Application.Interfaces;
public interface IFileService
{
    Task<Result<FileDto>> UploadFileAsync(IFormFile file, Guid userId, Guid? parentFolderId, Guid? fileEntryId = null);
    Task<Result<FileDto>> CreateFolderAsync(string name, Guid userId, Guid? parentFolderId);
    Task<Result<FileDto>> GetFileOrFolderAsync(Guid id, Guid userId);
    Task<Result<FileDto>> GetByShareLinkAsync(string shareLink, Guid userId);
    Task<Result<ShareResponse>> ShareFileOrFolderAsync(Guid id, Guid ownerId, Guid targetUserId, AccessLevel accessLevel);
    Task<Result<List<FileDto>>> ListFolderContentsAsync(Guid? folderId, Guid userId);
    Task<Result<List<FileVersionDto>>> GetFileVersionsAsync(Guid fileId, Guid userId);
    Task<Result<FileDto>> RestoreFileVersionAsync(Guid fileId, Guid userId, int versionNumber);
    Task DeleteFileOrFolderAsync(Guid id, Guid userId);
}
