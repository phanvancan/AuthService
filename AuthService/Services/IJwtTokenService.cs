using AuthService.Models;

namespace AuthService.Services;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(User user, string roleName);
    (string Token, DateTime ExpiresAt) GenerateRefreshToken(User user, string roleName);
}
