using System.ComponentModel.DataAnnotations;
using deblog.Server.Features.Users;

namespace deblog.Server.Features.Auth;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record LoginResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string? RefreshToken,
    UserProfileDto User
);
