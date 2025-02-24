namespace Domain.Entities;
public class FileVersion
{
    public Guid FileVersionId { get;  set; }
    public Guid FileEntryId { get;  set; }
    public FileEntry? FileEntry { get;  set; }
    public string? Name { get;  set; }
    public  string? FilePath { get;  set; }
    public long Size { get;  set; }
    public int VersionNumber { get;  set; }
    public DateTime CreatedAt { get;  set; }


    private FileVersion() { }
    public FileVersion(Guid fileEntryId, string name, string filePath, long size, int versionNumber)
    {
        FileVersionId = Guid.NewGuid();
        FileEntryId = fileEntryId;
        Name = name;
        FilePath = filePath;
        Size = size;
        VersionNumber = versionNumber;
        CreatedAt = DateTime.UtcNow;
    }
}
