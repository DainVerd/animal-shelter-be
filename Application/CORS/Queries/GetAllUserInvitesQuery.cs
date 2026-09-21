using Application.Dtos;
using Application.Entities;
using Application.Entities.Common;
using Application.Exceptions;
using Application.Interfaces.Services;
using Application.ViewModels;
using AutoMapper;
using MediatR;

namespace Application.CORS.Queries;

public class GetAllUserInvitesQuery : IRequest<PaginatedList<UserInviteDto>>
{
    public required PaginationParams Paging { get; init; }

    public required UserInviteFilterViewModel Filter { get; set; }
}
public class GetAllUserInvitesQueryHandler : IRequestHandler<GetAllUserInvitesQuery, PaginatedList<UserInviteDto>>
{
    private readonly IUserInviteService _userInviteService;
    private readonly IMapper _mapper;

    public GetAllUserInvitesQueryHandler(
       IUserInviteService userInviteService, IMapper mapper)
    {
        _userInviteService = userInviteService;
        _mapper = mapper;
    }

    public async Task<PaginatedList<UserInviteDto>> Handle(GetAllUserInvitesQuery request, CancellationToken cancellationToken)
    {
        if (request.Paging is null)
            throw new ArgumentNullException(nameof(request.Paging), ErrorMessages.GetArgumentMessage(ArgumentErrorCode.ArgumentIsEmpty));

        if (request.Filter is null)
            throw new ArgumentNullException(nameof(request.Filter), ErrorMessages.GetArgumentMessage(ArgumentErrorCode.ArgumentIsEmpty));

        var filter = _mapper.Map<UserInviteFilter>(request.Filter);

        var result = await _userInviteService.GetInvitesAsync(filter, request.Paging, cancellationToken);

        var userInviteDto = result.Items.Select(invite =>
        {
            return _mapper.Map<UserInviteDto>(invite);
        }).ToList();

        return new PaginatedList<UserInviteDto>(userInviteDto, result.TotalCount, request.Paging.PageNumber, request.Paging.PageSize);
    }
}
