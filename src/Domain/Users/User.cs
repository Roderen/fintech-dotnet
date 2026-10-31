namespace Domain.Users;

public class User
{
    public Guid Id { get; private set; }
    
    public string Email { get; private set; } = null!;
    public string Username { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Surname { get; private set; } = null!;
    
    public bool HasWallet { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private User() {}
    
    public User(string email, string username, string passwordHash, string name, string surname)
    {
        Id = Guid.NewGuid();
        Email = email;
        Username = username;
        PasswordHash = passwordHash;
        Name = name;
        Surname = surname;
        HasWallet = false;
        CreatedAt = DateTime.UtcNow;
    }
    
    public void MarkHasWallet(bool hasWallet)
    {
        HasWallet = hasWallet;
    }
}