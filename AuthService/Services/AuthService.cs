using AuthService.DTOs.Auth;
using AuthService.Models;
using AuthService.Repositories;

namespace AuthService.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(IUserRepository userRepository, IRoleRepository roleRepository, IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthTokenResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByUsernameAsync(request.Username)
            ?? throw new UnauthorizedAccessException("Invalid username or password.");

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Account is inactive.");
        }

        var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!passwordValid)
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        var roleName = user.UserRoles.FirstOrDefault()?.Role.RoleName ?? "User";

        var accessToken = _jwtTokenService.GenerateAccessToken(user, roleName);
        var refreshToken = _jwtTokenService.GenerateRefreshToken(user, roleName);

        user.RefreshToken = refreshToken.Token;
        user.RefreshTokenExpiresAt = refreshToken.ExpiresAt;
        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        return new AuthTokenResponse
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiresAt = refreshToken.ExpiresAt,
            Username = user.Username,
            Role = roleName
        };
    }

    public async Task<AuthTokenResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userRepository.GetByUsernameAsync(request.Username);
        if (existingUser is not null)
        {
            throw new InvalidOperationException("Username already exists.");
        }

        var userRole = await _roleRepository.GetByNameAsync("User")
            ?? throw new InvalidOperationException("Default role not found.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Email = request.Email,
            FullName = request.FullName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        user.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RoleId = userRole.Id
        });

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        return await LoginAsync(new LoginRequest
        {
            Username = request.Username,
            Password = request.Password
        });
    }

    public async Task<AuthTokenResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (user.RefreshTokenExpiresAt is null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Refresh token has expired.");
        }

        var roleName = user.UserRoles.FirstOrDefault()?.Role.RoleName ?? "User";
        var accessToken = _jwtTokenService.GenerateAccessToken(user, roleName);
        var refreshToken = _jwtTokenService.GenerateRefreshToken(user, roleName);

        user.RefreshToken = refreshToken.Token;
        user.RefreshTokenExpiresAt = refreshToken.ExpiresAt;
        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        return new AuthTokenResponse
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiresAt = refreshToken.ExpiresAt,
            Username = user.Username,
            Role = roleName
        };
    }
}
