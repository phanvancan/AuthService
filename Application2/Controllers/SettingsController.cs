using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Application2.Controllers;

[Authorize]
public class SettingsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
