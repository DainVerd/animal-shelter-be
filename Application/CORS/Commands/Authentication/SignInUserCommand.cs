using Application.Dtos;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Services;
using Application.ViewModels.Authentication;
using Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace Application.CORS.Commands.Authentication;

public class SignInUserCommand : IRequest<SignInResponseDto>
{
    [Required]
    public required SignInViewModel Model { get; init; }
}

public class SignInUserCommandHandler : IRequestHandler<SignInUserCommand, SignInResponseDto>
{

    private readonly IUserService _userService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<SignInUserCommand> _logger;
    public SignInUserCommandHandler(IUserService userService, IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, ILogger<SignInUserCommand> logger)
    {

        _userService = userService;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<SignInResponseDto> Handle(SignInUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userService.GetUserByEmailAsync(request.Model.Email);
        if (user is null)
            throw new NotFoundException(ErrorMessages.GetMessage(ErrorCode.UserNotFound));

        if (_httpContextAccessor?.HttpContext is null)
            throw new Exceptions.ValidationException(ErrorMessages.GetMessage(ErrorCode.NotFound));


        if (await _userService.IsLockedOutAsync(user))
        {
            _logger.LogWarning("User {modelEmail} is locked out.", request.Model.Email);

            throw new ApplicationException(ErrorMessages.GetMessage(ErrorCode.UserBlocked));
        }

        if (!await _userService.CheckPasswordAsync(user, request.Model.Password))
        {
            await _userService.IncrementAccessFailedCountAsync(user);

            throw new UnauthorizedException(ErrorMessages.GetMessage(ErrorCode.InvalidPassword));
        }

        await _userService.ResetAccessFailedCountAsync(user);

        var claimsToAdd = await _userService.GetClaimsForAccessTokenByUserIdAsync(user.Id);

        var accessToken = _jwtTokenService.GenerateAccessToken(claimsToAdd);
        var refreshToken = _jwtTokenService.GetRefreshToken();

        _unitOfWork.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAt,
            IsRevoked = false,
            UserId = user.Id
        }
        );

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _httpContextAccessor.HttpContext.Response.Cookies.Append("X-Refresh-Token", refreshToken.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = refreshToken.ExpiresAt
        });

        return new SignInResponseDto
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
        };
    }
}
