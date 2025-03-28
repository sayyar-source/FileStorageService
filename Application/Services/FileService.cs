﻿using Application.DTOs;
using Application.Interfaces;
using Domain.Commons;
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

    // Uploads a file, either new or as an update to an existing one
    public async Task<Result<FileDto>> UploadFileAsync(IFormFile file, Guid userId, Guid? parentFolderId, Guid? fileEntryId = null)
    {
        // Validate the file
        if (file == null || file.Length == 0)
            return Result<FileDto>.Failure("File cannot be empty.");

        try
        {
            // Build the folder path for Azure Blob Storage
            var folderPath = await BuildFolderPathAsync(parentFolderId);
            var blobName = string.IsNullOrEmpty(folderPath) ? file.FileName : $"{folderPath}/{file.FileName}";
            var path = await _storageService.UploadFileAsync(file, blobName);
            FileEntry existingFile = null!;
            var fileEntry = new FileEntry(file.FileName, path, file.ContentType, file.Length, userId);

            // If there’s a parent folder, check it exists and belongs to the user
            if (parentFolderId.HasValue)
            {
                var parentFolder = await _fileRepository.GetByIdAsync(parentFolderId.Value);
                if (parentFolder == null || !parentFolder.IsFolder)
                    return Result<FileDto>.Failure("Parent folder does not exist or is not a folder.");
                if (parentFolder.OwnerId != userId)
                    return Result<FileDto>.Failure("User does not own the parent folder.");
                fileEntry.ParentFolderId = parentFolderId;
            }

            // If updating an existing file
            if (fileEntryId.HasValue)
            {
                existingFile = await _fileRepository.GetByIdAsync(fileEntryId.Value);
                if (existingFile == null || existingFile.OwnerId != userId || existingFile.IsFolder)
                    return Result<FileDto>.Failure("User does not own the file or file is a folder.");

                // Update the existing file’s details with the new file’s info
                existingFile.Name = fileEntry.Name;
                existingFile.Path = fileEntry.Path;
                existingFile.ContentType = fileEntry.ContentType;
                existingFile.Size = fileEntry.Size;
                existingFile.UpdatedAt = DateTime.UtcNow; // Update the timestamp
                var currentVersion = existingFile.Versions.Max(v => v.VersionNumber);
                await _fileRepository.UpdateAsync(existingFile);
                var fileVersion = new FileVersion(existingFile.Id, existingFile.Name!, existingFile.Path!, existingFile.Size, currentVersion + 1);
                await _fileVersionRepository.AddAsync(fileVersion);

                _logger.LogInformation("File updated: {FileId} to {Path} by user {UserId}", existingFile.Id, path, userId);
                return Result<FileDto>.Success(MapToDto(existingFile)); // Return the updated file details
            }

            // If it’s a new file, save it and log the action
            await _fileRepository.AddAsync(fileEntry);
            _logger.LogInformation("File added: {FileId} to {Path} by user {UserId}", fileEntry.Id, path, userId);
            return Result<FileDto>.Success(MapToDto(fileEntry));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file for user {UserId}. File: {FileName}, ParentFolderId: {ParentFolderId}, FileEntryId: {FileEntryId}",
                userId, file?.FileName, parentFolderId, fileEntryId);
            return Result<FileDto>.Failure("An error occurred while uploading the file.");
        }
    }


    // Creates a new folder for organizing files
    public async Task<Result<FileDto>> CreateFolderAsync(string name, Guid userId, Guid? parentFolderId)
    {
        // Ensure the folder has a name
        if (string.IsNullOrEmpty(name))
            return Result<FileDto>.Failure("Folder name cannot be empty.");

        try
        {
            FileEntry parentFolder = null!;

            // Check the parent folder if one is specified
            if (parentFolderId.HasValue)
            {
                parentFolder = await _fileRepository.GetByIdAsync(parentFolderId.Value);
                if (parentFolder == null || !parentFolder.IsFolder)
                    return Result<FileDto>.Failure("Parent folder does not exist or is not a folder.");
                if (parentFolder.OwnerId != userId)
                    return Result<FileDto>.Failure("User does not own the parent folder.");
            }

            // Create and save the new folder
            var folder = new FileEntry(name, null!, null!, 0, userId, parentFolderId, true);
            if (parentFolder != null)
                parentFolder.Children.Add(folder); // Link it to the parent

            await _fileRepository.AddAsync(folder);
            _logger.LogInformation("Folder created: {FolderId} by user {UserId}", folder.Id, userId);
            return Result<FileDto>.Success(MapToDto(folder));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create folder for user {UserId}. Name: {Name}, ParentFolderId: {ParentFolderId}",
                userId, name, parentFolderId);
            return Result<FileDto>.Failure("An error occurred while creating the folder.");
        }
    }


    // Gets details about a file or folder, including its contents if it’s a folder
    public async Task<Result<FileDto>> GetFileOrFolderAsync(Guid id, Guid userId)
    {
        try
        {
            var entry = await _fileRepository.GetByIdAsync(id);

            // Check if the user owns it or has shared access
            if (entry == null || (entry.OwnerId != userId && !entry.SharedAccesses.Any(sa => sa.UserId == userId)))
                return Result<FileDto>.Failure("User does not have access to this file or folder.");

            var dto = MapToDto(entry);

            // If it’s a folder, include its children
            if (entry.IsFolder)
                dto.Children = (await _fileRepository.GetChildrenAsync(id, userId)).Select(MapToDto).ToList();

            return Result<FileDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve file or folder {Id} for user {UserId}", id, userId);
            return Result<FileDto>.Failure("An error occurred while retrieving the file or folder.");
        }
    }


    // Accesses a file or folder via a share link
    public async Task<Result<FileDto>> GetByShareLinkAsync(string shareLink, Guid userId)
    {
        try
        {
            var entry = await _fileRepository.GetByShareLinkAsync(shareLink);

            // Verify the user has access through the share link
            if (entry == null || !entry.SharedAccesses.Any(sa => sa.UserId == userId && sa.ShareLink == shareLink))
            {
                return Result<FileDto>.Failure("Invalid share link or no access.");
            }

            var dto = MapToDto(entry);

            // If it’s a folder, include its children
            if (entry.IsFolder)
            {
                dto.Children = (await _fileRepository.GetChildrenAsync(entry.Id, userId)).Select(MapToDto).ToList();
            }

            return Result<FileDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve entry by share link {ShareLink} for user {UserId}", shareLink, userId);
            return Result<FileDto>.Failure("An error occurred while retrieving the file or folder by share link.");
        }
    }

    // Shares a file or folder with another user
    public async Task<Result<ShareResponse>> ShareFileOrFolderAsync(Guid id, Guid ownerId, Guid targetUserId, AccessLevel accessLevel)
    {
        try
        {
            var entry = await _fileRepository.GetByIdAsync(id);
            if (entry == null || entry.OwnerId != ownerId)
                return Result<ShareResponse>.Failure("User does not own this file or folder.");

            var targetUser = await _userRepository.GetByIdAsync(targetUserId);
            // Check if the target user exists
            if (targetUser == null)
                return Result<ShareResponse>.Failure("Target user does not exist.");

            // Create and save the sharing permission
            var sharedAccess = new SharedAccess(entry.Id, targetUserId, accessLevel);
            await _sharedAccessRepository.AddAsync(sharedAccess);

            _logger.LogInformation("{Type} {Id} shared with user {TargetUserId} by {OwnerId}",
                entry.IsFolder ? "Folder" : "File", id, targetUserId, ownerId);

            return Result<ShareResponse>.Success(new ShareResponse { ShareLink = sharedAccess.ShareLink! });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to share file or folder {Id} from owner {OwnerId} to user {TargetUserId}", id, ownerId, targetUserId);
            return Result<ShareResponse>.Failure("An error occurred while sharing the file or folder.");
        }
    }

    // Lists all items inside a folder (or root if no folder is specified)
    public async Task<Result<List<FileDto>>> ListFolderContentsAsync(Guid? folderId, Guid userId)
    {
        try
        {
            if (folderId.HasValue)
            {
                var folder = await _fileRepository.GetByIdAsync(folderId.Value);
                if (folder == null || !folder.IsFolder)
                    return Result<List<FileDto>>.Failure("Specified ID does not correspond to a folder.");

                if (folder.OwnerId != userId && !folder.SharedAccesses.Any(sa => sa.UserId == userId))
                    return Result<List<FileDto>>.Failure("User does not have access to this folder.");
            }

            var contents = await _fileRepository.GetChildrenAsync(folderId, userId);
            var fileDtos = contents.Select(MapToDto).ToList();
            return Result<List<FileDto>>.Success(fileDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list contents of folder {FolderId} for user {UserId}", folderId, userId);
            return Result<List<FileDto>>.Failure("An error occurred while listing folder contents.");
        }
    }

    // Retrieves all versions of a file
    public async Task<Result<List<FileVersionDto>>> GetFileVersionsAsync(Guid fileId, Guid userId)
    {
        try
        {
            var file = await _fileRepository.GetByIdAsync(fileId);
            if (file == null || file.OwnerId != userId || file.IsFolder)
                return Result<List<FileVersionDto>>.Failure("User does not have access to this file or it is a folder.");

            var versions = await _fileRepository.GetVersionsAsync(fileId);
            var fileVersionDtos = versions.Select(v => new FileVersionDto
            {
                FileVersionId = v.FileVersionId,
                FileEntryId = v.FileEntryId,
                Name = v.Name!,
                Size = v.Size,
                VersionNumber = v.VersionNumber,
                CreatedAt = v.CreatedAt
            }).ToList();

            return Result<List<FileVersionDto>>.Success(fileVersionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve versions for file {FileId} for user {UserId}", fileId, userId);
            return Result<List<FileVersionDto>>.Failure("An error occurred while retrieving file versions.");
        }
    }

    // Restores a file to a previous version
    public async Task<Result<FileDto>> RestoreFileVersionAsync(Guid fileId, Guid userId, int versionNumber)
    {
        try
        {
            var file = await _fileRepository.GetByIdAsync(fileId);
            if (file == null || file.OwnerId != userId || file.IsFolder)
                return Result<FileDto>.Failure("User does not have access to this file or it is a folder.");

            // Find the version to restore
            var version = (await _fileRepository.GetVersionsAsync(fileId))
                .FirstOrDefault(v => v.VersionNumber == versionNumber);
            if (version == null)
                return Result<FileDto>.Failure("Version not found.");

            // Upload the version as a new blob and update the file
            var restoredFilePath = await _storageService.UploadVersionAsync(version);
            file.RestoreVersion(versionNumber, restoredFilePath, version.FilePath!.Split('.').Last(), version.Size);
            await _fileRepository.UpdateAsync(file);

            return Result<FileDto>.Success(MapToDto(file));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore version {VersionNumber} for file {FileId} by user {UserId}", versionNumber, fileId, userId);
            return Result<FileDto>.Failure("An error occurred while restoring the file version.");
        }
    }

    // Deletes a file or folder
    public async Task<Result<string>> DeleteFileOrFolderAsync(Guid id, Guid userId)
    {
        try
        {
            var entry = await _fileRepository.GetByIdAsync(id);
            if (entry == null || entry.OwnerId != userId)
                return Result<string>.Failure("User does not own this file or folder.");

            if (entry.IsFolder && entry.Children.Any())
                return Result<string>.Failure("Cannot delete a folder with contents. Delete children first.");

            // Remove any shared access and delete the file from storage if it’s not a folder
            entry.SharedAccesses.Clear();
            await _fileRepository.UpdateAsync(entry);

            if (!entry.IsFolder)
                await _storageService.DeleteFileAsync(entry.Path!);

            await _fileRepository.DeleteAsync(id);
            _logger.LogInformation("{Type} {Id} deleted by user {UserId}", entry.IsFolder ? "Folder" : "File", id, userId);

            return Result<string>.Success($"{(entry.IsFolder ? "Folder" : "File")} deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file or folder {Id} for user {UserId}", id, userId);
            return Result<string>.Failure("An error occurred while deleting the file or folder.");
        }
    }


    // Builds the full folder path (e.g., "parent/subfolder") for storage
    private async Task<string> BuildFolderPathAsync(Guid? parentFolderId)
    {
        if (!parentFolderId.HasValue) return string.Empty;
        try
        {
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build folder path for ParentFolderId: {ParentFolderId}", parentFolderId);
            throw new InvalidOperationException("An error occurred while building the folder path.", ex);
        }
    }

    // Converts a FileEntry object to a FileDto for responses
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

