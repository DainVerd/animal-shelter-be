using Application.Constants;
using Application.Exceptions;
using Application.Interfaces.Services;
using MediatR;
using System.Web.Mvc;

namespace Application.CORS.Queries;

public class GetAvailableInviteRolesQuery : IRequest<IEnumerable<SelectListItem>>
{
    public required int UserId { get; init; }
}

public class GetAvailableInviteRolesQueryHandler
    : IRequestHandler<GetAvailableInviteRolesQuery, IEnumerable<SelectListItem>>
{
    private readonly IUserService _userService;
    private readonly IRoleService _roleService;

    public GetAvailableInviteRolesQueryHandler(
        IUserService userService,
        IRoleService roleService)
    {
        _userService = userService;
        _roleService = roleService;
    }

    public async Task<IEnumerable<SelectListItem>> Handle(
        GetAvailableInviteRolesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0)
            throw new ArgumentException("User id is not valid!");

        var currentUser = await _userService.GetUserByIdAsync(request.UserId);

        if (currentUser is null)
            throw new NotFoundException(
                ErrorMessages.GetMessage(ErrorCode.UserNotFound));

        var currentUserRoles =
            await _roleService.GetRolesForUserAsync(currentUser);

        if (currentUserRoles.Count == 0)
            return [];

        IEnumerable<string> allowedRoleNames;

        if (currentUserRoles.Any(role => role.Name == UserRole.SuperAdmin))
        {
            allowedRoleNames = UserRole.GetRoleNames();
        }
        else if (currentUserRoles.Any(role => role.Name == UserRole.Admin))
        {
            allowedRoleNames =
            [
                UserRole.User,
                UserRole.ShelterWorker
            ];
        }
        else
        {
            return [];
        }

        var allRoles = await _roleService.GetAllRolesAsync();

        return allRoles
            .Where(role => allowedRoleNames.Contains(role.Name))
            .Select(role => new SelectListItem
            {
                Value = role.RoleCode.ToString(),
                Text = role.Name
            })
            .ToList();
    }
}