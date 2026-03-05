public record CreateItemRequest(int storeId, string name, string[]? searchTerms, List<int>? tagIds, decimal price = 0, string description = "", int stock = 0);
public record UpdateItemRequest(string? name, decimal? price, string? description, int? stock, string[]? searchTerms, List<int>? tagIds, List<int>? imageOrder);
