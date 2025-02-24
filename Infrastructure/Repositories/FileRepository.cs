using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;
public class FileRepository : IFileRepository
{
    private readonly AppDbContext _context;

    public FileRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<FileEntry> GetByIdAsync(Guid id)
    {
        try
        {
         return await _context.Files
        .Include(f => f.SharedAccesses)
        .Include(f => f.Versions)
        .Include(f => f.Children)
        .FirstOrDefaultAsync(f => f.Id == id);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to retrieve file entry.", ex);
        }

    }
    public async Task<List<FileEntry>> GetChildrenAsync(Guid? parentFolderId, Guid ownerId)
    {
        try
        {
            return await _context.Files
                .Where(f => f.OwnerId == ownerId && f.ParentFolderId == parentFolderId)
                .ToListAsync();
        }
        catch 
        {
            return new List<FileEntry>();
        }
    }
    public async Task<List<FileEntry>> GetByOwnerIdAsync(Guid ownerId, Guid? parentFolderId)
    {
        try
        {
            return await _context.Files
                .Where(f => f.OwnerId == ownerId && f.ParentFolderId == parentFolderId)
                .ToListAsync();
        }
        catch 
        {
            return new List<FileEntry>();
        }
    }

    public async Task<FileEntry> GetByShareLinkAsync(string shareLink)
    {
        try
        {
            return await _context.Files
                .Include(f => f.SharedAccesses)
                .Include(f => f.Children)
                .FirstOrDefaultAsync(f => f.SharedAccesses.Any(sa => sa.ShareLink == shareLink));
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message) ;
        }
    }

    public async Task AddAsync(FileEntry fileEntry)
    {
        try
        {
            _context.Files.Add(fileEntry);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to add file entry.", ex);
        }
    }

    public async Task UpdateAsync(FileEntry fileEntry)
    {
        try
        {
            var existingFileEntry = await _context.Files.FindAsync(fileEntry.Id);
            if (existingFileEntry == null)
            {
                throw new KeyNotFoundException("FileEntry not found.");
            }

            _context.Entry(existingFileEntry).CurrentValues.SetValues(fileEntry);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new InvalidOperationException("The file entry was modified by another process. Please reload and try again.", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to update file entry.", ex);
        }
    }

    public async Task DeleteAsync(Guid fileId)
    {
        try
        {
            var file = await _context.Files.FindAsync(fileId);
            if (file != null)
            {
                _context.Files.Remove(file);
                await _context.SaveChangesAsync();
            }
            else
            {
                throw new KeyNotFoundException("FileEntry not found.");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to delete file entry.", ex);
        }
    }
    public async Task<List<FileVersion>> GetVersionsAsync(Guid fileEntryId)
    {
        try
        {
            return await _context.FileVersions
                .Where(fv => fv.FileEntryId == fileEntryId)
                .OrderBy(fv => fv.VersionNumber)
                .ToListAsync();
        }
        catch
        {
            return new List<FileVersion>();
        }
    }
}