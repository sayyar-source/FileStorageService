namespace Application.DTOs;
public class FileVersionDto
{
    public Guid FileVersionId { get; set; }
    public Guid FileEntryId { get; set; }
    public string? Name { get; set; }
    public long Size { get; set; }
    public int VersionNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}
