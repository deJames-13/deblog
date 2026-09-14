namespace deblog.Server.Common.Security.RateLimiting;

/// <summary>
/// Canonical policy names for partitioned ASP.NET Core rate limiting.
/// </summary>
public static class RateLimitingPolicies
{
    /// <summary>
    /// Strict sliding window policy guarding authentication and login endpoints (5 requests / 5 minutes per IP).
    /// Prevents credential stuffing and brute-force password guessing.
    /// </summary>
    public const string AdminLogin = "admin-login";

    /// <summary>
    /// Fixed window policy guarding guest commenting (3 requests / 1 minute per IP).
    /// Prevents comment section flood and spam bots.
    /// </summary>
    public const string CommentSpam = "comment-spam";

    /// <summary>
    /// Sliding window policy guarding post reaction counters such as like and share (15 requests / 1 minute per IP).
    /// Prevents telemetry manipulation and rapid click abuse while allowing genuine reading activity.
    /// </summary>
    public const string ReactionSpam = "reaction-spam";

    /// <summary>
    /// Fixed window policy guarding transactional OTP generation (3 requests / 5 minutes per IP).
    /// Prevents exhaustion of transactional email provider limits.
    /// </summary>
    public const string OtpSpam = "otp-spam";

    /// <summary>
    /// Sliding window fallback policy across all public API routes (100 requests / 1 minute per IP).
    /// </summary>
    public const string Global = "global";
}
