using Domain.Entities;

namespace Domain.Interfaces;
public interface ISharedAccessRepository
{
    Task AddAsync(SharedAccess sharedAccess);
    Task<SharedAccess> GetByLinkAsync(string shareLink);
}