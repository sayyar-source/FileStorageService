using Application.Services;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests;
public class FileServiceTests
{
    private readonly Mock<IFileRepository> _fileRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ISharedAccessRepository> _sharedAccessRepositoryMock;
    private readonly Mock<IStorageService> _storageServiceMock;
    private readonly Mock<IFileVersionRepository> _fileVersionRepositoryMock;
    private readonly Mock<ILogger<FileService>> _loggerMock;
    private readonly FileService _fileService;

    public FileServiceTests()
    {
        _fileRepositoryMock = new Mock<IFileRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _sharedAccessRepositoryMock = new Mock<ISharedAccessRepository>();
        _storageServiceMock = new Mock<IStorageService>();
        _fileVersionRepositoryMock = new Mock<IFileVersionRepository>();
        _loggerMock = new Mock<ILogger<FileService>>();

        _fileService = new FileService(
            _fileRepositoryMock.Object,
            _userRepositoryMock.Object,
            _storageServiceMock.Object,
            _sharedAccessRepositoryMock.Object,
            _fileVersionRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullArguments_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new FileService(null!,
            _userRepositoryMock.Object, _storageServiceMock.Object,
            _sharedAccessRepositoryMock.Object, _fileVersionRepositoryMock.Object,
            _loggerMock.Object));
    }


    [Fact]
    public async Task UploadFileAsync_WithValidFile_ReturnsSuccessResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(100);
        fileMock.Setup(f => f.FileName).Returns("test.txt");
        fileMock.Setup(f => f.ContentType).Returns("text/plain");
        _storageServiceMock.Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
            .ReturnsAsync("path/to/test.txt");

        // Act
        var result = await _fileService.UploadFileAsync(fileMock.Object, userId, null);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("test.txt", result.Data.Name);
        Assert.Equal("path/to/test.txt", result.Data.Path);
        _fileRepositoryMock.Verify(r => r.AddAsync(It.IsAny<FileEntry>()), Times.Once());
    }

    [Fact]
    public async Task CreateFolderAsync_WithEmptyName_ReturnsFailureResult()
    {
        var result = await _fileService.CreateFolderAsync("", Guid.NewGuid(), null);
        Assert.False(result.IsSuccess);
        Assert.Equal("Folder name cannot be empty.", result.Error);
    }

    [Fact]
    public async Task CreateFolderAsync_WithValidInput_ReturnsSuccessResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var folderName = "NewFolder";

        // Act
        var result = await _fileService.CreateFolderAsync(folderName, userId, null);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(folderName, result.Data.Name);
        Assert.True(result.Data.IsFolder);
        _fileRepositoryMock.Verify(r => r.AddAsync(It.Is<FileEntry>(f =>
            f.Name == folderName && f.IsFolder)), Times.Once());
    }

    [Fact]
    public async Task ShareFileOrFolderAsync_WithValidInput_ReturnsSuccessResult()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var fileEntry = new FileEntry("test.txt", "path", "text/plain", 100, ownerId);
        _fileRepositoryMock.Setup(r => r.GetByIdAsync(fileId)).ReturnsAsync(fileEntry);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(targetUserId)).ReturnsAsync(new Domain.Entities.User("user1@gmail.com", "123"));

        // Act
        var result = await _fileService.ShareFileOrFolderAsync(fileId, ownerId, targetUserId, AccessLevel.View);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data.ShareLink);
        _sharedAccessRepositoryMock.Verify(r => r.AddAsync(It.IsAny<SharedAccess>()), Times.Once());
    }

    [Fact]
    public async Task DeleteFileOrFolderAsync_WithNonEmptyFolder_ThrowsInvalidOperationException()
    {
        // Arrange
        var folderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var folder = new FileEntry("folder", null!, null!, 0, userId, null, true);
        folder.Children.Add(new FileEntry("child", "path", "text/plain", 100, userId));
        _fileRepositoryMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _fileService.DeleteFileOrFolderAsync(folderId, userId));
    }

    [Fact]
    public async Task UploadFileAsync_UpdateExistingFile_ReturnsSuccessResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.Length).Returns(200);
        fileMock.Setup(f => f.FileName).Returns("updated.txt");
        fileMock.Setup(f => f.ContentType).Returns("text/plain");

        var existingFile = new FileEntry("original.txt", "old/path", "text/plain", 100, userId);
        existingFile.Versions.Add(new FileVersion(fileId, "original.txt", "old/path", 100, 1));
        _fileRepositoryMock.Setup(r => r.GetByIdAsync(fileId)).ReturnsAsync(existingFile);
        _storageServiceMock.Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
            .ReturnsAsync("new/path");

        // Act
        var result = await _fileService.UploadFileAsync(fileMock.Object, userId, null, fileId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("updated.txt", result.Data.Name);
        Assert.Equal("new/path", result.Data.Path);
        _fileRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<FileEntry>()), Times.Once());
        _fileVersionRepositoryMock.Verify(r => r.AddAsync(It.Is<FileVersion>(v => v.VersionNumber == 2)), Times.Once());
    }

    [Fact]
    public async Task GetByShareLinkAsync_WithValidLink_ReturnsSuccessResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        string shareLink = "valid-link";
        var fileEntry = new FileEntry("shared.txt", "path", "text/plain", 100, Guid.NewGuid());
        fileEntry.SharedAccesses.Add(new SharedAccess(fileId, userId, AccessLevel.View) { ShareLink = shareLink });
        _fileRepositoryMock.Setup(r => r.GetByShareLinkAsync(shareLink)).ReturnsAsync(fileEntry);

        // Act
        var result = await _fileService.GetByShareLinkAsync(shareLink, userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("shared.txt", result.Data.Name);
    }

    [Fact]
    public async Task ListFolderContentsAsync_WithValidFolder_ReturnsContents()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var folder = new FileEntry("folder", null!, null!, 0, userId, null, true);
        var childFile = new FileEntry("child.txt", "path", "text/plain", 100, userId);
        _fileRepositoryMock.Setup(r => r.GetByIdAsync(folderId)).ReturnsAsync(folder);
        _fileRepositoryMock.Setup(r => r.GetChildrenAsync(folderId, userId))
            .ReturnsAsync(new List<FileEntry> { childFile });

        // Act
        var result = await _fileService.ListFolderContentsAsync(folderId, userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Data);
        Assert.Equal("child.txt", result.Data[0].Name);
    }
}


