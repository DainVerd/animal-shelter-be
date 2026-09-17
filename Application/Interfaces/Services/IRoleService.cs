using Domain.Models;

namespace Application.Interfaces.Services;

public interface IRoleService
{
    Task<Role?> GetRoleByNameAsync(string roleName);
    Task<bool> RoleExistsAsync(string roleName);
    Task<IList<Role>> GetRolesForUserAsync(User user);
    Task<bool> IsUserInRoleAsync(User user, string roleName);
    Task CreateRoleAsync(Role roleToCreate);
    Task<IList<Role>> GetAllRolesAsync();
}
