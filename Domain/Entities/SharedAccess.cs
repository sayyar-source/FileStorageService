namespace Domain.Entities;
public class SharedAccess:BaseEntity
{
    public Guid FileEntryId { get;  set; }
    public FileEntry? FileEntry { get;  set; }
    public Guid UserId { get;  set; }
    public User? User { get;  set; }
    public AccessLevel AccessLevel { get;  set; }
    public string? ShareLink { get;  set; }

    private SharedAccess() { } 
    public SharedAccess(Guid fileEntryId, Guid userId, AccessLevel accessLevel)
    {
        FileEntryId = fileEntryId;
        UserId = userId;
        AccessLevel = accessLevel;
        ShareLink = Guid.NewGuid().ToString("N");
    }
}
