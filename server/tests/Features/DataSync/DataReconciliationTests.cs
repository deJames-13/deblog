using deblog.Server.Common.Data;
using deblog.Server.Common.Services;
using deblog.Server.Features.Comments;
using deblog.Server.Features.Users;
using deblog.Server.Tests.Common;
using Microsoft.Extensions.DependencyInjection;

namespace deblog.Server.Tests.Features.DataSync;

public class DataReconciliationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public DataReconciliationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ReconcileAsync_RemovesOrphanedComments_WhenParentPostDeletedInSupabase()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reconciler = scope.ServiceProvider.GetRequiredService<IDataReconciliationService>();

        // Arrange: Create an orphaned comment (PostId points to a non-existent post)
        var author = new User
        {
            Id = Guid.NewGuid(),
            Email = $"commenter_{Guid.NewGuid()}@test.local",
            Username = "commenter",
            DisplayName = "Commenter",
            Role = UserRoles.User,
        };
        db.Users.Add(author);

        var orphanedPostId = Guid.NewGuid(); // post doesn't exist in db.Posts
        var orphanedComment = new Comment
        {
            Id = Guid.NewGuid(),
            PostId = orphanedPostId,
            AuthorId = author.Id,
            Author = author,
            Content = "Orphaned comment content",
            Status = CommentStatus.Approved,
            CreatedAt = DateTime.UtcNow,
        };
        db.Comments.Add(orphanedComment);
        await db.SaveChangesAsync();

        // Act
        var result = await reconciler.ReconcileAsync();

        // Assert
        Assert.True(result.OrphanedCommentsHandled > 0);
        var commentInDb = await db.Comments.FindAsync(orphanedComment.Id);
        Assert.Null(commentInDb);
    }
}
