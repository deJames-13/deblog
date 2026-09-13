using System.Text.Json.Serialization;

namespace deblog.Server.Features.Comments;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CommentStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Spam = 3
}
