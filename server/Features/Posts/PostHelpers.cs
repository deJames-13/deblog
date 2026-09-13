using System.Text.RegularExpressions;
using deblog.Server.Features.Comments;

namespace deblog.Server.Features.Posts;

public static class PostHelpers
{
    public static string GenerateSlug(string text)
    {
        var slug = text.ToLowerInvariant().Trim();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString()[..8] : slug;
    }

    public static PostListItemDto ToListItemDto(Post p, string baseUrl)
    {
        return new PostListItemDto(
            p.Id,
            p.Title,
            p.Slug,
            p.Summary,
            p.Content,
            $"{baseUrl}/posts/{p.Slug}",
            p.Status,
            p.IsPublished,
            p.IsDeleted,
            p.DeletedAt,
            p.PublishedAt,
            p.CreatedAt,
            new AuthorSummaryDto(
                p.Author?.Id ?? Guid.Empty,
                p.Author?.Username ?? "author",
                p.Author?.DisplayName ?? "Author",
                p.Author?.AvatarUrl),
            new PostAnalyticsDto(
                p.Analytics != null ? p.Analytics.Views : 0,
                p.Analytics != null ? p.Analytics.Likes : 0,
                p.Analytics != null ? p.Analytics.Shares : 0,
                p.Comments != null ? p.Comments.Count(c => c.Status == CommentStatus.Approved) : 0
            ),
            p.CoverImageUrl,
            p.Category,
            p.Tags,
            p.IsFeatured
        );
    }

    public static PostDetailDto ToDetailDto(Post post, string baseUrl)
    {
        return new PostDetailDto(
            post.Id,
            post.Title,
            post.Slug,
            post.Summary,
            post.Content,
            $"{baseUrl}/posts/{post.Slug}",
            post.Status,
            post.IsPublished,
            post.IsDeleted,
            post.DeletedAt,
            post.PublishedAt,
            post.CreatedAt,
            post.UpdatedAt,
            new AuthorSummaryDto(
                post.Author?.Id ?? Guid.Empty,
                post.Author?.Username ?? "author",
                post.Author?.DisplayName ?? "Author",
                post.Author?.AvatarUrl),
            new PostAnalyticsDto(
                post.Analytics?.Views ?? 0,
                post.Analytics?.Likes ?? 0,
                post.Analytics?.Shares ?? 0,
                post.Comments != null ? post.Comments.Count(c => c.Status == CommentStatus.Approved) : 0
            ),
            post.CoverImageUrl,
            post.Category,
            post.Tags,
            post.IsFeatured
        );
    }
}
