using Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Storage;
public interface IStorageService
{
    Task<string> UploadFileAsync(IFormFile file, string blobName);
    Task<string> UploadVersionAsync(FileVersion version); // For restoring versions
    Task DeleteFileAsync(string path);
}
