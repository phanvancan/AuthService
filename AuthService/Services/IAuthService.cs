using AuthService.DTOs.Auth;

namespace AuthService.Services;

public interface IAuthService
{
    Task<AuthTokenResponse> LoginAsync(LoginRequest request);
    Task<AuthTokenResponse> RegisterAsync(RegisterRequest request);
    Task<AuthTokenResponse> RefreshTokenAsync(RefreshTokenRequest request);
}
