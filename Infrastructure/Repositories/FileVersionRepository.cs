using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;
public class FileVersionRepository : IFileVersionRepository
{
    private readonly AppDbContext _context;

    public FileVersionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<FileVersion> GetByIdAsync(Guid fileVersionId)
    {
        return await _context.FileVersions
            .Include(fv => fv.FileEntry)
            .FirstOrDefaultAsync(fv => fv.FileVersionId == fileVersionId)
            ?? throw new KeyNotFoundException($"FileVersion with ID {fileVersionId} not found.");
    }

    public async Task<List<FileVersion>> GetByFileEntryIdAsync(Guid fileEntryId)
    {
        return await _context.FileVersions
            .Where(fv => fv.FileEntryId == fileEntryId)
            .OrderBy(fv => fv.VersionNumber)
            .ToListAsync();
    }

    public async Task<FileVersion> GetLatestVersionAsync(Guid fileEntryId)
    {
        return await _context.FileVersions
            .Where(fv => fv.FileEntryId == fileEntryId)
            .OrderByDescending(fv => fv.VersionNumber)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"No versions found for FileEntry with ID {fileEntryId}.");
    }

    public async Task AddAsync(FileVersion fileVersion)
    {
        _context.FileVersions.Add(fileVersion);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(FileVersion fileVersion)
    {
        _context.FileVersions.Update(fileVersion);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid fileVersionId)
    {
        var fileVersion = await GetByIdAsync(fileVersionId);
        _context.FileVersions.Remove(fileVersion);
        await _context.SaveChangesAsync();
    }
}
