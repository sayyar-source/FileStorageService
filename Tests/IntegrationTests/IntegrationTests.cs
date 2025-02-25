using Application.Services;
using Azure.Storage.Blobs;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;

namespace IntegrationTests;

public class FileServiceIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly FileService _fileService;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly AppDbContext _dbContext;

    public FileServiceIntegrationTests(IntegrationTestFixture fixture)
    {
        _dbContext = fixture.DbContext;
        _blobServiceClient = fixture.BlobServiceClient;
        string blobStorageConnection = "UseDevelopmentStorage=true";
        var storageService = new AzureBlobStorageService(blobStorageConnection, fixture.StorageServiceLogger);

        _fileService = new FileService(
            new FileRepository(_dbContext),
            new UserRepository(_dbContext),
            storageService,
            new SharedAccessRepository(_dbContext),
            new FileVersionRepository(_dbContext),
            fixture.FileServiceLogger);
    }

    [Fact]
    public async Task UploadFileAsync_NewFile_SuccessfullyUploadsAndStores()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("testuser@gmail.com", "password123") { Id = userId };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var fileContent = "Test file content";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
        var formFile = new FormFile(stream, 0, stream.Length, "test", "test.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };

        // Act
        var result = await _fileService.UploadFileAsync(formFile, userId, null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test.txt", result.Data.Name);
        Assert.NotNull(result.Data.Path);

        var dbFile = await _dbContext.Files.FindAsync(result.Data.Id);
        Assert.NotNull(dbFile);
        Assert.Equal(userId, dbFile.OwnerId);
        Assert.Contains(dbFile, user.Files); // Verify navigation property

        var blobClient = _blobServiceClient.GetBlobContainerClient("files").GetBlobClient("test.txt");
        var blobContent = await blobClient.DownloadContentAsync();
        var downloadedContent = Encoding.UTF8.GetString(blobContent.Value.Content.ToArray());
        Assert.Equal(fileContent, downloadedContent);
    }

    [Fact]
    public async Task CreateFolderAsync_WithParentFolder_SuccessfullyCreates()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var parentFolderId = Guid.NewGuid();
        var user = new User("testuser@gmail.com", "password123") { Id = userId };
        var parentFolder = new FileEntry("parent", null!, null!, 0, userId, null, true) { Id = parentFolderId };
        user.Files.Add(parentFolder);
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _fileService.CreateFolderAsync("child", userId, parentFolderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("child", result.Data.Name);
        Assert.True(result.Data.IsFolder);
        Assert.Equal(parentFolderId, result.Data.ParentFolderId);

        var dbFolder = await _dbContext.Files.FindAsync(result.Data.Id);
        Assert.NotNull(dbFolder);
        Assert.Equal(parentFolderId, dbFolder.ParentFolderId);
        Assert.Contains(dbFolder, user.Files);
    }

    [Fact]
    public async Task ShareFileOrFolderAsync_AndAccessViaLink_Successful()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var owner = new User("owner@gmail.com", "password123") { Id = ownerId };
        var targetUser = new User("target@gmail.com", "password456") { Id = targetUserId };
        var file = new FileEntry("shared.txt", "path/to/shared.txt", "text/plain", 100, ownerId) { Id = fileId };
        owner.Files.Add(file);
        await _dbContext.Users.AddRangeAsync(owner, targetUser);
        await _dbContext.SaveChangesAsync();

        // Act
        var shareResponse = await _fileService.ShareFileOrFolderAsync(fileId, ownerId, targetUserId, AccessLevel.View);
        var accessedFile = await _fileService.GetByShareLinkAsync(shareResponse.Data.ShareLink!, targetUserId);

        // Assert
        Assert.NotNull(shareResponse.Data.ShareLink);
        Assert.Equal("shared.txt", accessedFile.Data.Name);
        Assert.Equal(fileId, accessedFile.Data.Id);

        var sharedAccess = await _dbContext.SharedAccesses
            .FirstOrDefaultAsync(sa => sa.FileEntryId == fileId && sa.UserId == targetUserId);
        Assert.NotNull(sharedAccess);
        Assert.Equal(shareResponse.Data.ShareLink, sharedAccess.ShareLink);
        Assert.Contains(sharedAccess, targetUser.SharedAccesses); // Verify navigation property
    }

    [Fact]
    public async Task DeleteFileOrFolderAsync_File_DeletesFromDbAndStorage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User("testuser@gmail.com", "password123") { Id = userId };
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        var fileContent = "Delete me";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));
        var formFile = new FormFile(stream, 0, stream.Length, "test", "delete.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };
        var uploadedFile = await _fileService.UploadFileAsync(formFile, userId, null);

        // Act
        await _fileService.DeleteFileOrFolderAsync(uploadedFile.Data.Id, userId);

        // Assert
        var dbFile = await _dbContext.Files.FindAsync(uploadedFile.Data.Id);
        Assert.Null(dbFile);
        Assert.DoesNotContain(user.Files, f => f.Id == uploadedFile.Data.Id); // Verify removed from navigation property

        var blobClient = _blobServiceClient.GetBlobContainerClient("files").GetBlobClient("delete.txt");
        var exists = await blobClient.ExistsAsync();
        Assert.False(exists.Value);
    }
}

public class IntegrationTestFixture : IDisposable
{
    public AppDbContext DbContext { get; }
    public BlobServiceClient BlobServiceClient { get; }
    public ILogger<FileService> FileServiceLogger { get; }
    public ILogger<AzureBlobStorageService> StorageServiceLogger { get; }

    public IntegrationTestFixture()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;
        DbContext = new AppDbContext(options);

        try
        {
            BlobServiceClient = new BlobServiceClient("UseDevelopmentStorage=true");
            BlobServiceClient.GetBlobContainerClient("files").CreateIfNotExists();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to connect to Azurite.", ex);
        }

        FileServiceLogger = new Mock<ILogger<FileService>>().Object;
        StorageServiceLogger = new Mock<ILogger<AzureBlobStorageService>>().Object;
    }

    public void Dispose()
    {
        DbContext.Dispose();
        try
        {
            BlobServiceClient.GetBlobContainerClient("files").DeleteIfExists();
        }
        catch { }
    }
}
