using Application.CORS.Queries;
using Application.Dtos;
using Application.Exceptions;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace WebApi.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/lookup")]
[ApiVersion("1.0")]
[Authorize]
public class LookupController : Controller
{
    private readonly IMediator _mediator;

    public LookupController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get enums values for dropdown lists
    /// </summary>
    /// <param name="enumName">enum name to whom get list of items</param>
    /// <returns>collection of SelectListItems</returns>
    [HttpGet("{enumName}")]
    [ProducesResponseType(typeof(List<SelectListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetEnumOptions(string enumName)
    {
        if (string.IsNullOrEmpty(enumName))
            throw new ArgumentException("enumValue was not provided.");

        var enumTypes = new Dictionary<string, Type>
        {
            { "gender", typeof(Domain.Enums.Gender) },
            { "size", typeof(Domain.Enums.AnimalSize) },
            { "temperament", typeof(Domain.Enums.Temperament) },
            { "invitestatus", typeof(Domain.Enums.InviteStatus) }
        };

        if (!enumTypes.TryGetValue(enumName.ToLower(), out var enumType))
            throw new NotFoundException($"Enum {enumName} was not found.");

        var selectListItems = Enum.GetValues(enumType)
            .Cast<Enum>()
            .Select(e => new SelectListItem
            {
                Value = Convert.ToInt32(e).ToString(),
                Text = e.ToString()
            })
            .ToList();

        var successResponse = new List<SelectListItem>(selectListItems);

        return Ok(successResponse);
    }

    [HttpGet("user-roles")]
    [ProducesResponseType(typeof(IEnumerable<SelectListItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAvailableInviteRoles(
    CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(
            Application.Constants.CustomClaimType.UserId);

        if (string.IsNullOrEmpty(userId))
            throw new UnauthorizedException("Claims does not contain userId!");

        if (!int.TryParse(userId, out var id))
            throw new ApplicationException("Failed to get id of current user");

        var result = await _mediator.Send(
            new GetAvailableInviteRolesQuery { UserId = id },
            cancellationToken);

        return Ok(result);
    }
}
