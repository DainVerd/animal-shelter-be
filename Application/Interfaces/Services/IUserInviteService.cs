using Application.Entities;
using Application.Entities.Common;
using Domain.Models;

namespace Application.Interfaces.Services;

public interface IUserInviteService
{
    Task<PaginatedList<UserInvite>> GetInvitesAsync(UserInviteFilter filter, PaginationParams pagination, CancellationToken ct = default);

}
