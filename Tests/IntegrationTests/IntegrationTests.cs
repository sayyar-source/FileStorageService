using Application.Services;
using Azure.Storage.Blobs;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
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

        var storageService = new AzureBlobStorageService(
            "DefaultEndpointsProtocol=https;AccountName=mystorage2000;AccountKey=CC7kFkxPuNoN4cUWJjR6UYu2dNm+ovyPfrA0hoSIE/YbAXWUcwi/6fr1TPbZhQBe7DrHdPw8uw4Y+AStq1vprQ==;EndpointSuffix=core.windows.net",
            fixture.StorageServiceLogger);

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
        var user = new User("testuser@gmail.com", "password123");
        user.Id = userId; 
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
        Assert.Equal("test.txt", result.Name);
        Assert.NotNull(result.Path);

        var dbFile = await _dbContext.Files.FindAsync(result.Id);
        Assert.NotNull(dbFile);
        Assert.Equal(userId, dbFile.OwnerId);
        Assert.Contains(dbFile, user.Files); // Verify navigation property

        var blobClient = _blobServiceClient.GetBlobContainerClient("files").GetBlobClient("test.txt");
        var blobContent = await blobClient.DownloadContentAsync();
        Assert.Equal(fileContent, blobContent.Value.Content.ToString());
    }

    [Fact]
    public async Task CreateFolderAsync_WithParentFolder_SuccessfullyCreates()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var parentFolderId = Guid.NewGuid();
        var user = new User("testuser@gmail.com", "password123") { Id = userId };
        var parentFolder = new FileEntry("parent", null, null, 0, userId, null, true) { Id = parentFolderId };
        user.Files.Add(parentFolder);
        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _fileService.CreateFolderAsync("child", userId, parentFolderId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("child", result.Name);
        Assert.True(result.IsFolder);
        Assert.Equal(parentFolderId, result.ParentFolderId);

        var dbFolder = await _dbContext.Files.FindAsync(result.Id);
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
        var accessedFile = await _fileService.GetByShareLinkAsync(shareResponse.ShareLink, targetUserId);

        // Assert
        Assert.NotNull(shareResponse.ShareLink);
        Assert.Equal("shared.txt", accessedFile.Name);
        Assert.Equal(fileId, accessedFile.Id);

        var sharedAccess = await _dbContext.SharedAccesses
            .FirstOrDefaultAsync(sa => sa.FileEntryId == fileId && sa.UserId == targetUserId);
        Assert.NotNull(sharedAccess);
        Assert.Equal(shareResponse.ShareLink, sharedAccess.ShareLink);
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
        await _fileService.DeleteFileOrFolderAsync(uploadedFile.Id, userId);

        // Assert
        var dbFile = await _dbContext.Files.FindAsync(uploadedFile.Id);
        Assert.Null(dbFile);
        Assert.DoesNotContain(user.Files, f => f.Id == uploadedFile.Id); // Verify removed from navigation property

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
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
            .EnableSensitiveDataLogging()
            .Options;
        DbContext = new AppDbContext(options);

        try
        {
            BlobServiceClient = new BlobServiceClient("DefaultEndpointsProtocol=https;AccountName=mystorage2000;AccountKey=CC7kFkxPuNoN4cUWJjR6UYu2dNm+ovyPfrA0hoSIE/YbAXWUcwi/6fr1TPbZhQBe7DrHdPw8uw4Y+AStq1vprQ==;EndpointSuffix=core.windows.net");
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
        try { BlobServiceClient.GetBlobContainerClient("files").DeleteIfExists(); } catch { }
    }
}

public class FileRepository : IFileRepository
{
    private readonly AppDbContext _context;
    public FileRepository(AppDbContext context) => _context = context;

    public Task<FileEntry?> GetByIdAsync(Guid id) =>
        _context.Files.Include(f => f.Versions).Include(f => f.SharedAccesses).FirstOrDefaultAsync(f => f.Id == id);

    public Task<List<FileEntry>> GetChildrenAsync(Guid? parentFolderId, Guid ownerId) =>
        Task.FromResult(_context.Files.Where(f => f.ParentFolderId == parentFolderId && f.OwnerId == ownerId).ToList());

    public Task<List<FileEntry>> GetByOwnerIdAsync(Guid ownerId, Guid? parentFolderId) =>
        Task.FromResult(_context.Files.Where(f => f.OwnerId == ownerId && f.ParentFolderId == parentFolderId).ToList());

    public Task<FileEntry?> GetByShareLinkAsync(string shareLink) =>
        _context.Files.Include(f => f.SharedAccesses).FirstOrDefaultAsync(f => f.SharedAccesses.Any(sa => sa.ShareLink == shareLink));

    public Task AddAsync(FileEntry fileEntry)
    { 
        _context.Files.Add(fileEntry);
        return _context.SaveChangesAsync(); 
    }

    public Task UpdateAsync(FileEntry fileEntry)
    { 
        _context.Files.Update(fileEntry);
        return _context.SaveChangesAsync();
    }

    public Task DeleteAsync(Guid fileId)
    {
        var file = _context.Files.Find(fileId);
        if (file != null) _context.Files.Remove(file);
        return _context.SaveChangesAsync();
    }

    public Task<List<FileVersion>> GetVersionsAsync(Guid fileEntryId) =>
        Task.FromResult(_context.FileVersions.Where(v => v.FileEntryId == fileEntryId).ToList());
}

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;
    public UserRepository(AppDbContext context) => _context = context;

    public Task<User?> GetByIdAsync(Guid id) =>
        _context.Users.Include(u => u.Files).Include(u => u.SharedAccesses).FirstOrDefaultAsync(u => u.Id == id);

    public Task<User?> GetByEmailAsync(string email) =>
        _context.Users.Include(u => u.Files).Include(u => u.SharedAccesses).FirstOrDefaultAsync(u => u.Email == email);

    public Task AddAsync(User user) 
    {
        _context.Users.Add(user); return _context.SaveChangesAsync();
    }
}

public class SharedAccessRepository : ISharedAccessRepository
{
    private readonly AppDbContext _context;
    public SharedAccessRepository(AppDbContext context) => _context = context;

    public Task AddAsync(SharedAccess sharedAccess)
    {
        _context.SharedAccesses.Add(sharedAccess);
        return _context.SaveChangesAsync(); 
    }

    public Task<SharedAccess?> GetByLinkAsync(string shareLink) =>
        _context.SharedAccesses.FirstOrDefaultAsync(sa => sa.ShareLink == shareLink);
}

public class FileVersionRepository : IFileVersionRepository
{
    private readonly AppDbContext _context;
    public FileVersionRepository(AppDbContext context) => _context = context;

    public Task<FileVersion?> GetByIdAsync(Guid fileVersionId) =>
        _context.FileVersions.FirstOrDefaultAsync(v => v.FileVersionId == fileVersionId);

    public Task<List<FileVersion>> GetByFileEntryIdAsync(Guid fileEntryId) =>
        Task.FromResult(_context.FileVersions.Where(v => v.FileEntryId == fileEntryId).ToList());

    public Task<FileVersion?> GetLatestVersionAsync(Guid fileEntryId) =>
        _context.FileVersions.Where(v => v.FileEntryId == fileEntryId)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync();

    public Task AddAsync(FileVersion fileVersion)
    { 
        _context.FileVersions.Add(fileVersion);
        return _context.SaveChangesAsync(); 
    }

    public Task UpdateAsync(FileVersion fileVersion) 
    {
        _context.FileVersions.Update(fileVersion);
        return _context.SaveChangesAsync(); 
    }

    public Task DeleteAsync(Guid fileVersionId)
    {
        var version = _context.FileVersions.Find(fileVersionId);
        if (version != null) _context.FileVersions.Remove(version);
        return _context.SaveChangesAsync();
    }
}