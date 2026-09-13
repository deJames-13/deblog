using System.Text.Json.Serialization;

namespace deblog.Server.Features.Posts;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PostStatus
{
    Draft = 0,
    Published = 1,
    Hidden = 2,
    Archived = 3
}
