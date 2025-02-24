namespace Domain.Entities;
public class User
{
    public Guid Id { get;  set; }
    public string? Email { get;  set; }
    public string? PasswordHash { get;  set; }
    public List<FileEntry?> Files { get;  set; } = [];
    public List<SharedAccess?> SharedAccesses { get;  set; } = [];

    private User() { } 
    public User(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email cannot be empty.", nameof(email));
        if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException("Password cannot be empty.", nameof(password));

        Id = Guid.NewGuid();
        Email = email;
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);  // Hash password using BCrypt
    }

    public bool VerifyPassword(string password) => BCrypt.Net.BCrypt.Verify(password, PasswordHash);
}
