using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Application1.Controllers;

public class AccountController : Controller
{
    private readonly IConfiguration _configuration;

    public AccountController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        var ssoLoginUrl = _configuration["AuthService:SsoLoginUrl"]
            ?? throw new InvalidOperationException("AuthService:SsoLoginUrl is missing.");
        var clientName = _configuration["AuthService:ClientName"] ?? "Application1";
        var finalReturnUrl = !string.IsNullOrWhiteSpace(returnUrl)
            ? returnUrl
            : $"{Request.Scheme}://{Request.Host}/";

        var redirectUrl = $"{ssoLoginUrl}?returnUrl={Uri.EscapeDataString(finalReturnUrl)}&app={Uri.EscapeDataString(clientName)}";
        return Redirect(redirectUrl);
    }

    [Authorize]
    public IActionResult Logout()
    {
        var ssoLogoutUrl = _configuration["AuthService:SsoLogoutUrl"]
            ?? throw new InvalidOperationException("AuthService:SsoLogoutUrl is missing.");
        var returnUrl = $"{Request.Scheme}://{Request.Host}/";
        return Redirect($"{ssoLogoutUrl}?returnUrl={Uri.EscapeDataString(returnUrl)}");
    }

    [Authorize]
    public IActionResult Profile()
    {
        return View();
    }

    [Authorize]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
