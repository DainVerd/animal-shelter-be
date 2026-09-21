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

    public Task<PaginatedList<UserInvite>> GetPagginatedListAsync(UserInviteFilter filter, PaginationParams pagination, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsQueryable()
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


        // TODO: implement sorting by columns
        //query = pagination.SortBy?.ToLower() switch
        //{
        //    "name" => pagination.IsDescending ? query.OrderByDescending(a => a.Name) : query.OrderBy(a => a.Name),
        //    "breed" => pagination.IsDescending ? query.OrderByDescending(a => a.Breed) : query.OrderBy(a => a.Breed),
        //    "date" => pagination.IsDescending ? query.OrderByDescending(a => a.AdmissionDate) : query.OrderBy(a => a.AdmissionDate),
        //    "gender" => pagination.IsDescending ? query.OrderByDescending(a => a.Gender) : query.OrderBy(a => a.Gender),
        //    "dateofbirth" => pagination.IsDescending ? query.OrderByDescending(a => a.DateOfBirth) : query.OrderBy(a => a.DateOfBirth),
        //    _ => query.OrderBy(a => a.Id)
        //};

        return PaginatedList<UserInvite>.CreateAsync(query, pagination.PageNumber, pagination.PageSize, cancellationToken);
    }
}
