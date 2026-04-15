public class FileUrlProvider
{
    private readonly string _baseUrl;

    public FileUrlProvider(IConfiguration config)
    {
        _baseUrl = config["FileSettings:BaseUrl"] ?? "http://localhost:5000";
    }

    public string GetItemImageUrl(string fileName) => $"{_baseUrl}/item_images/{fileName}";
    public string GetLogoUrl(string? fileName) => string.IsNullOrEmpty(fileName) ? "" : $"{_baseUrl}/logos/{fileName}";
}