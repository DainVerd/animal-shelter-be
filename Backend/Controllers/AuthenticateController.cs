using Application.CORS.Commands.Authentication;
using Application.Dtos;
using Application.Exceptions;
using Application.Interfaces.Services;
using Application.ViewModels;
using Application.ViewModels.Authentication;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace WebApi.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/authenticate")]
[ApiVersion("1.0")]
[Authorize]
public class AuthenticateController : ControllerBase
{
    private readonly ILogger<AuthenticateController> _logger;
    private readonly IMediator _mediator;
    private readonly IUserService _userService;
    private readonly IPasswordService _passwordService;

    public AuthenticateController(
        ILogger<AuthenticateController> logger, IMediator mediator,
        IUserService userService, IPasswordService passwordService)
    {
        _logger = logger;
        _mediator = mediator;
        _userService = userService;
        _passwordService = passwordService;
    }

    [HttpPost]
    [Route("sign-in")]
    [ProducesResponseType(typeof(SignInResponseDto), StatusCodes.Status200OK)]
    [AllowAnonymous]
    [EnableRateLimiting("FixedPolicy")]
    public async Task<IActionResult> SignIn([FromBody] SignInViewModel model, CancellationToken cancellationToken)
    {
        var passwordValidationErrors = await _passwordService.ValidatePasswordAsync(model.Password);
        if (passwordValidationErrors.Count > 0)
            throw new ValidationException(passwordValidationErrors);

        var response = await _mediator.Send(new SignInUserCommand { Model = model }, cancellationToken);

        return Ok(response);
    }

    [HttpDelete("sign-out")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SignOut(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Revoke called");

        var username = HttpContext.User.Identity?.Name;

        if (username is null)
            throw new NotFoundException(ErrorMessages.GetMessage(ErrorCode.UserNotFound));

        await _mediator.Send(new SignOutUserCommand(), cancellationToken);

        Response.Cookies.Delete("X-Refresh-Token");

        return Ok();
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SignInResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies["X-Refresh-Token"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogWarning("Refresh token cookie not found");
            throw new UnauthorizedException("Refresh token cookie not found");
        }

        var response = await _mediator.Send(command, cancellationToken);

        return Ok(response);
    }

    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordViewModel request)
    {
        var command = new ChangePasswordCommand(request.OldPassword, request.NewPassword);
        await _mediator.Send(command);

        return Ok();
    }
}
