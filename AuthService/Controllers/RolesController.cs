using AuthService.Data;
using AuthService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class RolesController : ControllerBase
{
    private readonly AuthDbContext _context;

    public RolesController(AuthDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _context.Roles.OrderBy(r => r.RoleName).ToListAsync();
        return Ok(roles);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRole([FromBody] Role request)
    {
        if (string.IsNullOrWhiteSpace(request.RoleName))
        {
            return BadRequest(new { message = "RoleName is required." });
        }

        var exists = await _context.Roles.AnyAsync(r => r.RoleName == request.RoleName);
        if (exists)
        {
            return BadRequest(new { message = "Role already exists." });
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            RoleName = request.RoleName,
            Description = request.Description
        };

        await _context.Roles.AddAsync(role);
        await _context.SaveChangesAsync();

        return Ok(role);
    }

    [HttpPost("assign")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
    {
        var user = await _context.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == request.UserId);
        if (user is null)
        {
            return NotFound(new { message = "User not found." });
        }

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId);
        if (role is null)
        {
            return NotFound(new { message = "Role not found." });
        }

        var current = user.UserRoles.FirstOrDefault();
        if (current is null)
        {
            user.UserRoles.Add(new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleId = role.Id
            });
        }
        else
        {
            current.RoleId = role.Id;
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Role assigned." });
    }

    public class AssignRoleRequest
    {
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
    }
}
