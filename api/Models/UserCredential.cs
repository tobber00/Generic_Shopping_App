public class UserCredential
{
    public int Id { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    
    // Foreign Key to User
    public int UserId { get; set; }
    public User? User { get; set; }
}