using Application.Constants;
using Application.Exceptions;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Models;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Services;

public class RoleService : IRoleService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IMapper _mapper;
    public RoleService(UserManager<User> userManager, RoleManager<Role> roleManager, IMapper mapper)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _mapper = mapper;
    }

    public async Task CreateRoleAsync(Role roleToCreate)
    {
        await _roleManager.CreateAsync(roleToCreate);
    }


    public async Task<Role?> GetRoleByNameAsync(string roleName)
    {
        if (string.IsNullOrEmpty(roleName))
            throw new ArgumentException(ErrorMessages.GetMessage(ErrorCode.ArgumentIsEmpty), nameof(roleName));

        var appRole = await _roleManager.FindByNameAsync(roleName);
        if (appRole == null)
            return null;

        return _mapper.Map<Role>(appRole);
    }

    public async Task<IList<Role>> GetRolesForUserAsync(User user)
    {
        if (user is null)
            throw new ArgumentNullException(nameof(user), ErrorMessages.GetMessage(ErrorCode.ArgumentIsEmpty));

        var appUser = await _userManager.FindByIdAsync(user.Id.ToString());
        if (appUser == null)
            return [];

        var roleNames = await _userManager.GetRolesAsync(appUser);
        var roles = new List<Role>();
        foreach (var roleName in roleNames)
        {
            var appRole = await _roleManager.FindByNameAsync(roleName);
            if (appRole != null)
                roles.Add(_mapper.Map<Role>(appRole));
        }
        return roles;
    }

    public async Task<bool> IsUserInRoleAsync(User user, string roleName)
    {
        if (string.IsNullOrEmpty(roleName))
            throw new ArgumentException(ErrorMessages.GetMessage(ErrorCode.ArgumentIsEmpty), nameof(roleName));

        if (user is null)
            throw new ArgumentNullException(nameof(user), ErrorMessages.GetMessage(ErrorCode.ArgumentIsEmpty));

        var appUser = _mapper.Map<User>(user);

        return await _userManager.IsInRoleAsync(appUser, roleName);
    }

    public async Task<bool> RoleExistsAsync(string roleName)
    {
        if (string.IsNullOrEmpty(roleName))
            throw new ArgumentException(ErrorMessages.GetMessage(ErrorCode.ArgumentIsEmpty), nameof(roleName));

        return await _roleManager.RoleExistsAsync(roleName);
    }

    public async Task<IList<Role>> GetAllRolesAsync()
    {
        var roles = new List<Role>();

        foreach (var roleName in UserRole.GetRoleNames())
        {
            var role = await _roleManager.FindByNameAsync(roleName);

            if (role is not null)
                roles.Add(_mapper.Map<Role>(role));
        }

        return roles;
    }
}
