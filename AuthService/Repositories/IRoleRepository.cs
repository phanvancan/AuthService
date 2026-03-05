using AuthService.Models;

namespace AuthService.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string roleName);
    Task<List<Role>> GetAllAsync();
    Task AddAsync(Role role);
    Task SaveChangesAsync();
}
