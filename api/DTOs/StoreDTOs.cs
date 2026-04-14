public record CreateStoreRequest(string name, string description = "");
public record UpdateStoreRequest(string? name, string? description = "");