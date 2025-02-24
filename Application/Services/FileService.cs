using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
namespace Application.Services;
public class FileService : IFileService
{
    private readonly IFileRepository _fileRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISharedAccessRepository _sharedAccessRepository;
    private readonly IStorageService _storageService;
    private readonly IFileVersionRepository _fileVersionRepository;
    private readonly ILogger<FileService> _logger;
    public FileService(
        IFileRepository fileRepository,
        IUserRepository userRepository,
        IStorageService storageService,
        ISharedAccessRepository sharedAccessRepository,
        IFileVersionRepository fileVersionRepository,
        ILogger<FileService> logger)
      
    {
        _fileRepository = fileRepository ?? throw new ArgumentNullException(nameof(fileRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _fileVersionRepository = fileVersionRepository ?? throw new ArgumentNullException(nameof(fileVersionRepository));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _sharedAccessRepository = sharedAccessRepository ?? throw new ArgumentNullException(nameof(sharedAccessRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<FileDto> UploadFileAsync(IFormFile file, Guid userId, Guid? parentFolderId, Guid? fileEntryId = null)
    {
        if (file == null || file.Length == 0) throw new ArgumentException("File cannot be empty.", nameof(file));

        // Build the folder path for Azure Blob Storage
        var folderPath = await BuildFolderPathAsync(parentFolderId);
        var blobName = string.IsNullOrEmpty(folderPath) ? file.FileName : $"{folderPath}/{file.FileName}";
        var path = await _storageService.UploadFileAsync(file, blobName);
        FileEntry existingFile = null!;
        FileEntry fileEntry = new FileEntry(file.FileName, path, file.ContentType, file.Length, userId);
        if (parentFolderId.HasValue)
        {
            var parentFolder = await _fileRepository.GetByIdAsync(parentFolderId.Value);
            if (parentFolder == null || !parentFolder.IsFolder)
                throw new ArgumentException("Parent folder does not exist or is not a folder.");
            if (parentFolder.OwnerId != userId)
                throw new UnauthorizedAccessException("User does not own the parent folder.");
            fileEntry.ParentFolderId = parentFolderId;
        }
        // Check if updating an existing file
        if (fileEntryId.HasValue)
        {
            existingFile = await _fileRepository.GetByIdAsync(fileEntryId.Value);
            if (existingFile == null || existingFile.OwnerId != userId || existingFile.IsFolder)
                throw new UnauthorizedAccessException("User does not own the file or file is a folder.");

            existingFile.Name = fileEntry.Name;
            existingFile.Path = fileEntry.Path;
            existingFile.ContentType = fileEntry.ContentType;
            existingFile.Size = fileEntry.Size;
            existingFile.UpdatedAt = fileEntry.UpdatedAt;
            var currentVersion = existingFile.Versions.Max(v => v.VersionNumber);
            await _fileRepository.UpdateAsync(existingFile);
            var fileVersion = new FileVersion(existingFile.Id, existingFile.Name!, existingFile.Path!, existingFile.Size, currentVersion + 1);
            await _fileVersionRepository.AddAsync(fileVersion);
            _logger.LogInformation("File updated: {FileId} to {Path} by user {UserId}", existingFile!.Id, path, userId);
            return MapToDto(existingFile);
        }
        await _fileRepository.AddAsync(fileEntry);
        _logger.LogInformation("File added: {FileId} to {Path} by user {UserId}", fileEntry!.Id, path, userId);
        return MapToDto(fileEntry);
    }

    public async Task<FileDto> CreateFolderAsync(string name, Guid userId, Guid? parentFolderId)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentException("Folder name cannot be empty.", nameof(name));

        FileEntry parentFolder = null!;
        if (parentFolderId.HasValue)
        {
            parentFolder = await _fileRepository.GetByIdAsync(parentFolderId.Value);
            if (parentFolder == null || !parentFolder.IsFolder)
                throw new ArgumentException("Parent folder does not exist or is not a folder.");
            if (parentFolder.OwnerId != userId)
                throw new UnauthorizedAccessException("User does not own the parent folder.");
        }

        var folder = new FileEntry(name, null, null, 0, userId, parentFolderId, true);
        if (parentFolder != null)
            parentFolder.Children.Add(folder);

        await _fileRepository.AddAsync(folder);
        _logger.LogInformation("Folder created: {FolderId} by user {UserId}", folder.Id, userId);
        return MapToDto(folder);
    }

    public async Task<FileDto> GetFileOrFolderAsync(Guid id, Guid userId)
    {
        var entry = await _fileRepository.GetByIdAsync(id);
        if (entry == null || (entry.OwnerId != userId && !entry.SharedAccesses.Any(sa => sa.UserId == userId)))
            throw new UnauthorizedAccessException("User does not have access to this file or folder.");

        var dto = MapToDto(entry);
        if (entry.IsFolder)
            dto.Children = (await _fileRepository.GetChildrenAsync(id, userId)).Select(MapToDto).ToList();

        return dto;
    }

    public async Task<FileDto> GetByShareLinkAsync(string shareLink, Guid userId)
    {
        var entry = await _fileRepository.GetByShareLinkAsync(shareLink);
        if (entry == null || !entry.SharedAccesses.Any(sa => sa.UserId == userId && sa.ShareLink == shareLink))
            throw new UnauthorizedAccessException("Invalid share link or no access.");

        var dto = MapToDto(entry);
        if (entry.IsFolder)
            dto.Children = (await _fileRepository.GetChildrenAsync(entry.Id, userId)).Select(MapToDto).ToList();

        return dto;
    }

    public async Task<ShareResponse> ShareFileOrFolderAsync(Guid id, Guid ownerId, Guid targetUserId, AccessLevel accessLevel)
    {
        var entry = await _fileRepository.GetByIdAsync(id);
        if (entry == null || entry.OwnerId != ownerId)
            throw new UnauthorizedAccessException("User does not own this file or folder.");

        var targetUser = await _userRepository.GetByIdAsync(targetUserId);
        if (targetUser == null)
            throw new ArgumentException("Target user does not exist.", nameof(targetUserId));

        var sharedAccess = new SharedAccess(entry.Id, targetUserId, accessLevel);
        await _sharedAccessRepository.AddAsync(sharedAccess);
        //entry.SharedAccesses.Add(sharedAccess);
        // await _fileRepository.UpdateAsync(entry);

        _logger.LogInformation("{Type} {Id} shared with user {TargetUserId} via link {ShareLink} by {OwnerId}",
            entry.IsFolder ? "Folder" : "File", id, targetUserId, sharedAccess.ShareLink, ownerId);

        return new ShareResponse { ShareLink = sharedAccess.ShareLink };
    }

    public async Task<List<FileDto>> ListFolderContentsAsync(Guid? folderId, Guid userId)
    {
        if (folderId.HasValue)
        {
            var folder = await _fileRepository.GetByIdAsync(folderId.Value);
            if (folder == null || !folder.IsFolder)
                throw new ArgumentException("Specified ID does not correspond to a folder.");
            if (folder.OwnerId != userId && !folder.SharedAccesses.Any(sa => sa.UserId == userId))
                throw new UnauthorizedAccessException("User does not have access to this folder.");
        }

        var contents = await _fileRepository.GetChildrenAsync(folderId, userId);
        return contents.Select(MapToDto).ToList();
    }

    public async Task<List<FileVersionDto>> GetFileVersionsAsync(Guid fileId, Guid userId)
    {
        var file = await _fileRepository.GetByIdAsync(fileId);
        if (file == null || file.OwnerId != userId || file.IsFolder)
            throw new UnauthorizedAccessException();

        var versions = await _fileRepository.GetVersionsAsync(fileId);
        return versions.Select(v => new FileVersionDto
        {
            FileVersionId = v.FileVersionId,
            FileEntryId = v.FileEntryId,
            Name = v.Name,
            Size = v.Size,
            VersionNumber = v.VersionNumber,
            CreatedAt = v.CreatedAt
        }).ToList();
    }

    public async Task<FileDto> RestoreFileVersionAsync(Guid fileId, Guid userId, int versionNumber)
    {
        var file = await _fileRepository.GetByIdAsync(fileId);
        if (file == null || file.OwnerId != userId || file.IsFolder)
            throw new UnauthorizedAccessException();

        var version = (await _fileRepository.GetVersionsAsync(fileId))
            .FirstOrDefault(v => v.VersionNumber == versionNumber);
        if (version == null)
            throw new KeyNotFoundException("Version not found.");

        // With Azure, we can reuse the existing blob URL or create a new one
        var restoredFilePath = await _storageService.UploadVersionAsync(version); // Creates a new blob for consistency
        file.RestoreVersion(versionNumber, restoredFilePath, version.FilePath.Split('.').Last(), version.Size);
        await _fileRepository.UpdateAsync(file);

        return MapToDto(file);
    }

    public async Task DeleteFileOrFolderAsync(Guid id, Guid userId)
    {
        var entry = await _fileRepository.GetByIdAsync(id);
        if (entry == null || entry.OwnerId != userId)
            throw new UnauthorizedAccessException("User does not own this file or folder.");

        if (entry.IsFolder && entry.Children.Any())
            throw new InvalidOperationException("Cannot delete a folder with contents. Delete children first.");

        entry.SharedAccesses.Clear();
        await _fileRepository.UpdateAsync(entry);

        if (!entry.IsFolder)
            await _storageService.DeleteFileAsync(entry.Path!);

        await _fileRepository.DeleteAsync(id);
        _logger.LogInformation("{Type} {Id} deleted by user {UserId}", entry.IsFolder ? "Folder" : "File", id, userId);
    }

    // Helper method to build folder path
    private async Task<string> BuildFolderPathAsync(Guid? parentFolderId)
    {
        if (!parentFolderId.HasValue) return string.Empty;

        var pathParts = new List<string>();
        var currentId = parentFolderId.Value;

        while (currentId != Guid.Empty)
        {
            var folder = await _fileRepository.GetByIdAsync(currentId);
            if (folder == null || !folder.IsFolder) break;
            pathParts.Insert(0, folder.Name!);
            currentId = folder.ParentFolderId ?? Guid.Empty;
        }

        return string.Join("/", pathParts);
    }
    private FileDto MapToDto(FileEntry file) => new()
    {
        Id = file.Id,
        Name = file.Name,
        Path = file.Path,
        ContentType = file.ContentType,
        Size = file.Size,
        ParentFolderId = file.ParentFolderId,
        IsFolder = file.IsFolder
    };
}

