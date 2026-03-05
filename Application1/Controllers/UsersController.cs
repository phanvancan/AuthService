using Application1.Models;
using Application1.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Application1.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly AuthApiClient _authApiClient;

    public UsersController(AuthApiClient authApiClient)
    {
        _authApiClient = authApiClient;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _authApiClient.GetUsersAsync();
        return View(users);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserInputModel input)
    {
        if (!ModelState.IsValid)
        {
            var users = await _authApiClient.GetUsersAsync();
            return View("Index", users);
        }

        await _authApiClient.CreateUserAsync(input);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _authApiClient.DeleteUserAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
