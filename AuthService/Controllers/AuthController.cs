using AuthService.DTOs.Auth;
using AuthService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            SetAuthCookies(result);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(request);
            SetAuthCookies(result);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("refresh-token-cookie")]
    public async Task<IActionResult> RefreshTokenFromCookie()
    {
        var refreshCookieName = _configuration["Sso:RefreshTokenCookieName"] ?? "refresh_token";
        var refreshToken = Request.Cookies[refreshCookieName];
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized(new { message = "Missing refresh token cookie." });
        }

        try
        {
            var result = await _authService.RefreshTokenAsync(new RefreshTokenRequest { RefreshToken = refreshToken });
            SetAuthCookies(result);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        DeleteAuthCookies();
        return NoContent();
    }

    private void SetAuthCookies(AuthTokenResponse token)
    {
        var accessCookieName = _configuration["Sso:AccessTokenCookieName"] ?? "access_token";
        var refreshCookieName = _configuration["Sso:RefreshTokenCookieName"] ?? "refresh_token";
        var cookieDomain = _configuration["Sso:CookieDomain"];

        var accessOptions = CreateCookieOptions(token.AccessTokenExpiresAt, cookieDomain);
        var refreshOptions = CreateCookieOptions(token.RefreshTokenExpiresAt, cookieDomain);

        Response.Cookies.Append(accessCookieName, token.AccessToken, accessOptions);
        Response.Cookies.Append(refreshCookieName, token.RefreshToken, refreshOptions);
    }

    private void DeleteAuthCookies()
    {
        var accessCookieName = _configuration["Sso:AccessTokenCookieName"] ?? "access_token";
        var refreshCookieName = _configuration["Sso:RefreshTokenCookieName"] ?? "refresh_token";
        var cookieDomain = _configuration["Sso:CookieDomain"];

        var options = CreateCookieOptions(DateTime.UtcNow.AddDays(-1), cookieDomain);
        Response.Cookies.Delete(accessCookieName, options);
        Response.Cookies.Delete(refreshCookieName, options);
    }

    private static CookieOptions CreateCookieOptions(DateTime expiresAtUtc, string? cookieDomain)
    {
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = expiresAtUtc,
            Path = "/"
        };

        if (!string.IsNullOrWhiteSpace(cookieDomain))
        {
            options.Domain = cookieDomain;
        }

        return options;
    }
}
