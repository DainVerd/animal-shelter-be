using Application.Entities;
using Application.Entities.Common;
using Domain.Models;

namespace Application.Interfaces.Repositories;

public interface IUserInviteRepository : IGenericRepository<UserInvite>
{
    Task<UserInvite?> GetPendingByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);
    Task<UserInvite?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<PaginatedList<UserInvite>> GetPaginatedListAsync(UserInviteFilter filter, PaginationParams pagination, CancellationToken cancellationToken = default);
}
