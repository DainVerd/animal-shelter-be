using Application.Entities;
using Application.Entities.Common;
using Application.Interfaces.Repositories;
using Domain.Enums;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistance.Repositories;

public class UserInviteRepository : GenericRepository<UserInvite>, IUserInviteRepository
{
    public UserInviteRepository(DbContext context) : base(context)
    {
    }

    public async Task<UserInvite?> GetPendingByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(email))
            throw new ArgumentNullException(nameof(email));

        return await _dbSet
            .FirstOrDefaultAsync(
                x =>
                    x.Email == email &&
                    x.Status == InviteStatus.Pending &&
                    x.ExpiresAt > DateTimeOffset.UtcNow,
                cancellationToken);
    }

    public async Task<UserInvite?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _dbSet
        .FirstOrDefaultAsync(
            x => x.Token == tokenHash,
            cancellationToken);
    }

    public Task<PaginatedList<UserInvite>> GetPaginatedListAsync(UserInviteFilter filter, PaginationParams pagination, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsQueryable()
            .Include(x => x.InvitedByUser)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.EmailSearchText))
            query = query.Where(a => a.Email != null && a.Email.Contains(filter.EmailSearchText));

        if (!string.IsNullOrWhiteSpace(filter.RoleInclude))
            query = query.Where(a => a.Roles.Contains(filter.RoleInclude));

        if (filter.ExpiresFrom.HasValue)
            query = query.Where(a => a.ExpiresAt >= filter.ExpiresFrom);

        if (filter.ExpiresTo.HasValue)
            query = query.Where(a => a.ExpiresAt <= filter.ExpiresTo);

        if (filter.AcceptedFrom.HasValue)
            query = query.Where(a => a.AcceptedAt >= filter.AcceptedFrom);

        if (filter.AcceptedTo.HasValue)
            query = query.Where(a => a.AcceptedAt <= filter.AcceptedTo);

        if (filter.Status.HasValue)
            query = query.Where(a => a.Status == filter.Status);

        if (filter.CreatedFrom.HasValue)
            query = query.Where(a => a.CreatedAt >= filter.CreatedFrom);

        if (filter.CreatedTo.HasValue)
            query = query.Where(a => a.CreatedAt <= filter.CreatedTo);


        query = pagination.SortBy?.ToLower() switch
        {
            "email" => pagination.IsDescending ? query.OrderByDescending(a => a.Email) : query.OrderBy(a => a.Email),
            "status" => pagination.IsDescending ? query.OrderByDescending(a => a.Status) : query.OrderBy(a => a.Status),
            "invitedBy" => pagination.IsDescending ? query.OrderByDescending(a => a.InvitedByUser.UserName) : query.OrderBy(a => a.InvitedByUser.UserName),
            "createdAt" => pagination.IsDescending ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt),
            "expiresAt" => pagination.IsDescending ? query.OrderByDescending(a => a.ExpiresAt) : query.OrderBy(a => a.ExpiresAt),
            _ => query.OrderBy(a => a.Id)
        };

        return PaginatedList<UserInvite>.CreateAsync(query, pagination.PageNumber, pagination.PageSize, cancellationToken);
    }
}
