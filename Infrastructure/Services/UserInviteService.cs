using Application.Entities;
using Application.Entities.Common;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Models;

namespace Infrastructure.Services;

public class UserInviteService : IUserInviteService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    public UserInviteService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }
    public async Task<PaginatedList<UserInvite>> GetInvitesAsync(UserInviteFilter filter, PaginationParams pagination, CancellationToken ct = default)
    {
        if (filter is null)
            throw new ArgumentNullException(nameof(filter), ErrorMessages.GetArgumentMessage(ArgumentErrorCode.ArgumentIsEmpty));

        if (pagination is null)
            throw new ArgumentNullException(nameof(pagination), ErrorMessages.GetArgumentMessage(ArgumentErrorCode.ArgumentIsEmpty));

        return await _uow.UserInvites.GetPagginatedListAsync(filter, pagination, ct);
    }
}
