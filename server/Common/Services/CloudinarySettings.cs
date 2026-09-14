namespace deblog.Server.Common.Services;

public class CloudinarySettings
{
    public string? CloudName { get; set; }
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? Url { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Url) ||
        (!string.IsNullOrWhiteSpace(CloudName) &&
         !string.IsNullOrWhiteSpace(ApiKey) &&
         !string.IsNullOrWhiteSpace(ApiSecret));
}
