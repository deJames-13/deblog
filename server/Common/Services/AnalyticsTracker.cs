using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;

namespace deblog.Server.Common.Services;

public interface IAnalyticsTracker
{
    bool CanTrackView(string clientIdentifier, Guid postId);
    bool CanTrackLike(string clientIdentifier, Guid postId);
    bool CanTrackShare(string clientIdentifier, Guid postId);
    string GetClientIdentifier(HttpContext context);
}

public class MemoryAnalyticsTracker : IAnalyticsTracker
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan ViewCooldown = TimeSpan.FromHours(1);
    private static readonly TimeSpan LikeCooldown = TimeSpan.FromHours(24);
    private static readonly TimeSpan ShareCooldown = TimeSpan.FromMinutes(10);

    public MemoryAnalyticsTracker(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool CanTrackView(string clientIdentifier, Guid postId)
    {
        var key = $"analytics:view:{postId}:{clientIdentifier}";
        if (_cache.TryGetValue(key, out _))
        {
            return false;
        }

        _cache.Set(key, true, ViewCooldown);
        return true;
    }

    public bool CanTrackLike(string clientIdentifier, Guid postId)
    {
        var key = $"analytics:like:{postId}:{clientIdentifier}";
        if (_cache.TryGetValue(key, out _))
        {
            return false;
        }

        _cache.Set(key, true, LikeCooldown);
        return true;
    }

    public bool CanTrackShare(string clientIdentifier, Guid postId)
    {
        var key = $"analytics:share:{postId}:{clientIdentifier}";
        if (_cache.TryGetValue(key, out _))
        {
            return false;
        }

        _cache.Set(key, true, ShareCooldown);
        return true;
    }

    public string GetClientIdentifier(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var raw = $"{ip}|{userAgent}";

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash)[..16];
    }
}
