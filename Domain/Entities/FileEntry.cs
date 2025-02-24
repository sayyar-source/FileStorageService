namespace Domain.Entities;
public class FileEntry:BaseEntity
{
    public string? Name { get;  set; }
    public string? Path { get;  set; } // Azure Blob Storage URL for files, null for folders
    public string? ContentType { get;  set; }
    public long Size { get;  set; }
    public Guid OwnerId { get;  set; }
    public User Owner { get;  set; }
    public Guid? ParentFolderId { get;  set; }
    public FileEntry? ParentFolder { get;  set; }
    public List<FileEntry> Children { get;  set; } = new();
    public List<SharedAccess> SharedAccesses { get;  set; } = new();
    public List<FileVersion> Versions { get;  set; } = new();
    public bool IsFolder { get;  set; }

    private FileEntry() { } 
    public FileEntry(string name, string path, string contentType, long size, Guid ownerId, Guid? parentFolderId = null, bool isFolder = false)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentException("Name cannot be empty.", nameof(name));
        Name = name;
        Path = isFolder ? null : path;
        ContentType = isFolder ? "folder" : contentType;
        Size = isFolder ? 0 : size;
        OwnerId = ownerId;
        ParentFolderId = parentFolderId;
        IsFolder = isFolder;
        Versions.Add(new FileVersion(Id, name, path, size, 1));
    }

    public void RestoreVersion(int versionNumber, string path, string contentType, long size)
    {
        if (IsFolder) throw new InvalidOperationException("Cannot restore a folder.");
        var versionToRestore = Versions.FirstOrDefault(v => v.VersionNumber == versionNumber);
        if (versionToRestore == null) throw new InvalidOperationException("Version not found.");
        Name = versionToRestore.Name;
        Path = path;
        ContentType = contentType;
        Size = size;
        UpdatedAt = DateTime.UtcNow;
    }
}
