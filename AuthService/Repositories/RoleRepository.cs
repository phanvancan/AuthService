using AuthService.Data;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly AuthDbContext _context;

    public RoleRepository(AuthDbContext context)
    {
        _context = context;
    }

    public Task<Role?> GetByNameAsync(string roleName)
    {
        return _context.Roles.FirstOrDefaultAsync(x => x.RoleName == roleName);
    }

    public Task<List<Role>> GetAllAsync()
    {
        return _context.Roles.OrderBy(x => x.RoleName).ToListAsync();
    }

    public async Task AddAsync(Role role)
    {
        await _context.Roles.AddAsync(role);
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }
}
