namespace Application.DTOs;
public class FileDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Path { get; set; }
    public string? ContentType { get; set; }
    public long Size { get; set; }
    public Guid? ParentFolderId { get; set; }
    public bool IsFolder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<FileDto> Children { get; set; } = new(); // For hierarchical representation
}
