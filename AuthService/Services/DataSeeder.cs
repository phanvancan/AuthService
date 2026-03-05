using AuthService.Data;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Services;

public class DataSeeder : IDataSeeder
{
    private readonly AuthDbContext _context;

    public DataSeeder(AuthDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        if (!await _context.Roles.AnyAsync())
        {
            var adminRole = new Role { Id = Guid.NewGuid(), RoleName = "Admin", Description = "System administrator" };
            var userRole = new Role { Id = Guid.NewGuid(), RoleName = "User", Description = "Standard user" };

            await _context.Roles.AddRangeAsync(adminRole, userRole);

            var permissions = new[]
            {
                new Permission { Id = Guid.NewGuid(), PermissionName = "Users.Read", Description = "View users" },
                new Permission { Id = Guid.NewGuid(), PermissionName = "Users.Write", Description = "Manage users" }
            };

            await _context.Permissions.AddRangeAsync(permissions);
            await _context.SaveChangesAsync();

            await _context.RolePermissions.AddRangeAsync(
                new RolePermission { Id = Guid.NewGuid(), RoleId = adminRole.Id, PermissionId = permissions[0].Id },
                new RolePermission { Id = Guid.NewGuid(), RoleId = adminRole.Id, PermissionId = permissions[1].Id },
                new RolePermission { Id = Guid.NewGuid(), RoleId = userRole.Id, PermissionId = permissions[0].Id }
            );

            await _context.SaveChangesAsync();
        }

        if (!await _context.Users.AnyAsync(x => x.Username == "admin"))
        {
            var adminRole = await _context.Roles.FirstAsync(x => x.RoleName == "Admin");

            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Email = "admin@local.dev",
                FullName = "System Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            adminUser.UserRoles.Add(new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = adminUser.Id,
                RoleId = adminRole.Id
            });

            await _context.Users.AddAsync(adminUser);
            await _context.SaveChangesAsync();
        }
    }
}
