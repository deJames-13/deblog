using System.Text.Json.Serialization;

namespace deblog.Server.Features.Users;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserStatus
{
    Active = 0,
    Suspended = 1,
    Banned = 2
}
