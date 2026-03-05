using AuthService.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthService.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public (string Token, DateTime ExpiresAt) GenerateAccessToken(User user, string roleName)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(GetAccessTokenMinutes());
        var claims = BuildClaims(user, roleName, "access");
        return (GenerateToken(claims, expiresAt), expiresAt);
    }

    public (string Token, DateTime ExpiresAt) GenerateRefreshToken(User user, string roleName)
    {
        var expiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenDays());
        var claims = BuildClaims(user, roleName, "refresh");
        return (GenerateToken(claims, expiresAt), expiresAt);
    }

    private string GenerateToken(IEnumerable<Claim> claims, DateTime expiresAt)
    {
        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing.");
        var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is missing.");
        var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is missing.");

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static List<Claim> BuildClaims(User user, string roleName, string tokenType)
    {
        return new List<Claim>
        {
            new("UserId", user.Id.ToString()),
            new("Username", user.Username),
            new("Role", roleName),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, roleName),
            new("token_type", tokenType)
        };
    }

    private int GetAccessTokenMinutes()
    {
        return int.TryParse(_configuration["Jwt:AccessTokenMinutes"], out var minutes) ? minutes : 60;
    }

    private int GetRefreshTokenDays()
    {
        return int.TryParse(_configuration["Jwt:RefreshTokenDays"], out var days) ? days : 7;
    }
}
