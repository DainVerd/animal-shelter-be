using Application.Dtos;
using Application.Exceptions;
using Application.Interfaces;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Application.CORS.Commands.Authentication;

public class RefreshTokenCommand : IRequest<SignInResponseDto>
{
    public string? ActiveRole { get; init; }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, SignInResponseDto>
{
    private readonly IUserService _userService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RefreshTokenCommandHandler(
        IUserService userService, IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork, IMapper mapper,
        IHttpContextAccessor httpContextAccessor)
    {
        _userService = userService;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }
    public async Task<SignInResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (_httpContextAccessor?.HttpContext is null)
            throw new UnauthorizedException(ErrorMessages.GetMessage(ErrorCode.NotFound));

        if (!_httpContextAccessor.HttpContext.Request.Cookies.TryGetValue("X-Refresh-Token", out var refreshToken) || string.IsNullOrEmpty(refreshToken))
            throw new UnauthorizedException(ErrorMessages.GetMessage(ErrorCode.RefreshTokenInvalid));

        var storedToken = await _unitOfWork.RefreshTokens.GetRefreshTokenAsync(refreshToken, cancellationToken);
        if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedException(ErrorMessages.GetMessage(ErrorCode.RefreshTokenInvalid));

        var user = await _userService.GetUserByIdAsync(storedToken.UserId);
        if (user is null)
            throw new NotFoundException(ErrorMessages.GetMessage(ErrorCode.UserNotFound));

        if (!string.IsNullOrEmpty(request.ActiveRole))
        {
            var isUserInRole = await _userService.IsInRoleAsync(user, request.ActiveRole);
            if (!isUserInRole)
                throw new ApplicationException(ErrorMessages.GetMessage(ErrorCode.OperationFailed));
        }

        var claimsToAdd = await _userService.GetClaimsForAccessTokenByUserIdAsync(user.Id, request.ActiveRole);

        var newAccessToken = _jwtTokenService.GenerateAccessToken(claimsToAdd);
        var newRefreshToken = _jwtTokenService.GetRefreshToken();

        var refreshTokenToAdd = _mapper.Map<RefreshToken>(newRefreshToken);
        refreshTokenToAdd.UserId = user.Id;
        storedToken.IsRevoked = true;

        await _unitOfWork.RefreshTokens.AddAsync(refreshTokenToAdd, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _httpContextAccessor.HttpContext.Response.Cookies.Append("X-Refresh-Token", refreshTokenToAdd.Token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = refreshTokenToAdd.ExpiresAt
        });

        return
            new SignInResponseDto
            {
                AccessToken = newAccessToken.Token,
                AccessTokenExpiresAt = newAccessToken.ExpiresAt
            };
    }
}