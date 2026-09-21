using Application.Constants;
using Application.CORS.Commands;
using Application.CORS.Queries;
using Application.Dtos;
using Application.Entities.Common;
using Application.Exceptions;
using Application.ViewModels;
using Application.ViewModels.Animal;
using Application.ViewModels.User;
using Asp.Versioning;
using Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace WebApi.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/invites")]
[ApiVersion("1.0")]
[Authorize]
public class InviteController : Controller
{
    private readonly IMediator _mediator;

    public InviteController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("")]
    [ProducesResponseType(typeof(int), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [Authorize(Roles = $"{UserRole.SuperAdmin}, {UserRole.Admin}")]
    public async Task<IActionResult> SendInvite(
    [FromBody] SendInviteViewModel model,
    CancellationToken cancellationToken = default)
    {
        var userIdClaim = User.FindFirstValue(CustomClaimType.UserId);

        if (string.IsNullOrWhiteSpace(userIdClaim))
            throw new UnauthorizedException(
                "Claims does not contain userId.");

        if (!int.TryParse(userIdClaim, out var invitedByUserId))
            throw new UnauthorizedException(
                "Invalid userId claim.");

        var inviteId = await _mediator.Send(
            new SendInviteCommand
            {
                Email = model.Email,
                Roles = model.Roles,
                InvitedByUserId = invitedByUserId
            },
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            inviteId);
    }

    [HttpPost("accept")]
    [ProducesResponseType(typeof(SignInResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [AllowAnonymous]
    [EnableRateLimiting("FixedPolicy")]
    public async Task<IActionResult> AcceptInvite(
    [FromBody] AcceptInviteCommand command)
    {
        var result = await _mediator.Send(command);

        return Ok(result);
    }

    [HttpGet("")]
    [ProducesResponseType(typeof(PaginatedList<UserInviteDto>), StatusCodes.Status200OK)]
    //[Authorize(Roles = $"{UserRole.SuperAdmin}, {UserRole.Admin}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetUserInvites(
    [FromQuery] PaginationParams paging,
    [FromQuery] UserInviteFilterViewModel filter,
    CancellationToken cancellationToken = default)
    {
        var query = new GetAllUserInvitesQuery { Filter = filter, Paging = paging };
        var response = await _mediator.Send(query, cancellationToken);

        return Ok(response);
    }

}
