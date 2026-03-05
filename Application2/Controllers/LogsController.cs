using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Application2.Controllers;

[Authorize]
public class LogsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
