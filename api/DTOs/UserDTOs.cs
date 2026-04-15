public record RegisterRequest(string username, string email, string password);
public record LoginRequest(string username, string password);