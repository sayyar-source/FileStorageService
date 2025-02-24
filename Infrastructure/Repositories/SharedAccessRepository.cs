using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;
public class SharedAccessRepository : ISharedAccessRepository
{
    private readonly AppDbContext _context;

    public SharedAccessRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SharedAccess sharedAccess)
    {
        _context.SharedAccesses.Add(sharedAccess);
        await _context.SaveChangesAsync();
    }

    public async Task<SharedAccess> GetByLinkAsync(string shareLink)
    {
        return await _context.SharedAccesses
            .Include(s => s.FileEntry)
            .FirstOrDefaultAsync(s => s.ShareLink == shareLink);
    }
}