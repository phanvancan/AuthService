using AuthService.DTOs.Users;
using AuthService.Models;
using AuthService.Repositories;

namespace AuthService.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;

    public UserService(IUserRepository userRepository, IRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
    }

    public async Task<List<UserResponse>> GetAllAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(MapUser).ToList();
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request)
    {
        var existingUser = await _userRepository.GetByUsernameAsync(request.Username);
        if (existingUser is not null)
        {
            throw new InvalidOperationException("Username already exists.");
        }

        var role = await _roleRepository.GetByNameAsync(request.Role)
            ?? throw new InvalidOperationException("Role not found.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Email = request.Email,
            FullName = request.FullName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        user.UserRoles.Add(new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RoleId = role.Id
        });

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        var createdUser = await _userRepository.GetByIdAsync(user.Id) ?? user;
        return MapUser(createdUser);
    }

    public async Task<UserResponse?> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
        {
            return null;
        }

        user.Email = request.Email;
        user.FullName = request.FullName;
        user.IsActive = request.IsActive;

        var role = await _roleRepository.GetByNameAsync(request.Role)
            ?? throw new InvalidOperationException("Role not found.");

        var existingRole = user.UserRoles.FirstOrDefault();
        if (existingRole is null)
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
            existingRole.RoleId = role.Id;
        }

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        var updated = await _userRepository.GetByIdAsync(id) ?? user;
        return MapUser(updated);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
        {
            return false;
        }

        await _userRepository.DeleteAsync(user);
        await _userRepository.SaveChangesAsync();
        return true;
    }

    private static UserResponse MapUser(User user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            Role = user.UserRoles.FirstOrDefault()?.Role.RoleName ?? "User"
        };
    }
}
