namespace Domain.Entities;
public class SharedAccess:BaseEntity
{
    public Guid FileEntryId { get; private set; }
    public FileEntry FileEntry { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private set; }
    public AccessLevel AccessLevel { get; private set; }
    public string ShareLink { get; private set; }

    private SharedAccess() { } 
    public SharedAccess(Guid fileEntryId, Guid userId, AccessLevel accessLevel)
    {
        FileEntryId = fileEntryId;
        UserId = userId;
        AccessLevel = accessLevel;
        ShareLink = Guid.NewGuid().ToString("N");
    }
}
