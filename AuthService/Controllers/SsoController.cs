using AuthService.DTOs.Auth;
using AuthService.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace AuthService.Controllers;

[Route("sso")]
public class SsoController : Controller
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    public SsoController(IAuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
    }

    [HttpGet("login")]
    public IActionResult Login([FromQuery] string returnUrl, [FromQuery] string? app)
    {
        if (!IsAllowedReturnUrl(returnUrl))
        {
            return BadRequest("Invalid returnUrl");
        }

        var html = $$"""
<!DOCTYPE html>
<html lang="vi">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>AuthService - Đăng nhập SSO</title>
    <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet">
</head>
<body class="bg-light">
    <div class="container py-5">
        <div class="row justify-content-center">
            <div class="col-md-7 col-lg-5">
                <div class="card shadow-sm border-0">
                    <div class="card-body p-4">
                        <h4 class="mb-1">Đăng nhập tập trung SSO</h4>
                        <p class="text-muted mb-4">Ứng dụng: <strong>{{WebUtility.HtmlEncode(app ?? "Unknown")}}</strong></p>

                        <form method="post" action="/sso/login">
                            <input type="hidden" name="returnUrl" value="{{WebUtility.HtmlEncode(returnUrl)}}" />
                            <input type="hidden" name="app" value="{{WebUtility.HtmlEncode(app ?? string.Empty)}}" />

                            <div class="mb-3">
                                <label class="form-label">Username</label>
                                <input type="text" name="username" class="form-control" required />
                            </div>

                            <div class="mb-3">
                                <label class="form-label">Password</label>
                                <input type="password" name="password" class="form-control" required />
                            </div>

                            <button type="submit" class="btn btn-primary w-100">Đăng nhập</button>
                        </form>

                        <div class="alert alert-info mt-4 mb-0" role="alert">
                            Tài khoản mẫu: <strong>admin</strong> / <strong>Admin@123</strong>
                        </div>
                    </div>
                </div>
                <p class="text-center text-muted mt-3 mb-0">AuthService • Secure SSO</p>
            </div>
        </div>
    </div>
</body>
</html>
""";

        return Content(html, "text/html");
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginPost([FromForm] string username, [FromForm] string password, [FromForm] string returnUrl, [FromForm] string? app)
    {
        if (!IsAllowedReturnUrl(returnUrl))
        {
            return BadRequest("Invalid returnUrl");
        }

        try
        {
            var result = await _authService.LoginAsync(new LoginRequest
            {
                Username = username,
                Password = password
            });

            SetAuthCookies(result);
            return Redirect(returnUrl);
        }
        catch
        {
            return Content("<div style='font-family:Segoe UI;padding:20px'><h3>Đăng nhập thất bại</h3><p>Thông tin đăng nhập không hợp lệ.</p><a href='javascript:history.back()'>Quay lại</a></div>", "text/html");
        }
    }

    [HttpGet("logout")]
    public IActionResult Logout([FromQuery] string? returnUrl = null)
    {
        DeleteAuthCookies();

        if (!string.IsNullOrWhiteSpace(returnUrl) && IsAllowedReturnUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return Content("<p>Đã đăng xuất khỏi hệ thống SSO.</p>", "text/html");
    }

    private bool IsAllowedReturnUrl(string returnUrl)
    {
        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var allowedHosts = _configuration.GetSection("Sso:AllowedReturnHosts").Get<string[]>() ?? Array.Empty<string>();
        return allowedHosts.Any(x => string.Equals(x, uri.Authority, StringComparison.OrdinalIgnoreCase));
    }

    private void SetAuthCookies(AuthTokenResponse token)
    {
        var accessCookieName = _configuration["Sso:AccessTokenCookieName"] ?? "access_token";
        var refreshCookieName = _configuration["Sso:RefreshTokenCookieName"] ?? "refresh_token";
        var cookieDomain = _configuration["Sso:CookieDomain"];

        Response.Cookies.Append(accessCookieName, token.AccessToken, CreateCookieOptions(token.AccessTokenExpiresAt, cookieDomain));
        Response.Cookies.Append(refreshCookieName, token.RefreshToken, CreateCookieOptions(token.RefreshTokenExpiresAt, cookieDomain));
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
