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
            $"{baseUrl}/posts/{p.Slug}",
            p.Status,
            p.IsPublished,
            p.IsDeleted,
            p.DeletedAt,
            p.PublishedAt,
            p.CreatedAt,
            new AuthorSummaryDto(p.Author.Id, p.Author.Username, p.Author.DisplayName, p.Author.AvatarUrl),
            new PostAnalyticsDto(
                p.Analytics != null ? p.Analytics.Views : 0,
                p.Analytics != null ? p.Analytics.Likes : 0,
                p.Analytics != null ? p.Analytics.Shares : 0,
                p.Comments.Count(c => c.Status == CommentStatus.Approved)
            )
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
            new AuthorSummaryDto(post.Author.Id, post.Author.Username, post.Author.DisplayName, post.Author.AvatarUrl),
            new PostAnalyticsDto(
                post.Analytics?.Views ?? 0,
                post.Analytics?.Likes ?? 0,
                post.Analytics?.Shares ?? 0,
                post.Comments.Count(c => c.Status == CommentStatus.Approved)
            )
        );
    }
}
