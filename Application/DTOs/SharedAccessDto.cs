using Domain.Entities;

namespace Application.DTOs;
public class SharedAccessDto
{
    public Guid Id { get; set; }
    public Guid FileEntryId { get; set; }
    public Guid UserId { get; set; }
    public AccessLevel AccessLevel { get; set; }
    public string? ShareLink { get; set; }
}