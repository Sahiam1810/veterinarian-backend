using Application.Common.Abstractions;
using Application.Common.Results;
using Application.Permissions.UseCases;
using Application.Security.Abstractions;
using Application.Security.Errors;
using Application.Security.Models;
using Application.UserTokens.Abstraction;
using Application.Users.Abstraction;
using Infrastructure.Security.Options;
using Infrastructure.Security.Tokens;
using Domain.Roles;
using MediatR;
using Microsoft.Extensions.Options;
using UserEntity = Domain.Users.Entities.Users;
using UserTokenEntity = Domain.UserTokens.Entities.UserTokens;

namespace Infrastructure.Security.Authentication;

public sealed class AuthenticationService(
    IUsersRepository usersRepository,
    IUserTokensRepository userTokenRepository,
    IUnitOfWork unitOfWork,
    ISender sender,
    JwtTokenIssuer jwtTokenIssuer,
    RefreshTokenProtector refreshTokenProtector,
    IPasswordHasher passwordHasher,
    IOptions<JwtOptions> options,
    TimeProvider timeProvider) : IAuthenticationService
{
    private const string RefreshTokenType = "refresh";

    private readonly JwtOptions jwtOptions = options.Value;

    public async Task<Result<AuthenticationTokens>> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        var user = await usersRepository.GetByEmailAsync(
            normalizedEmail, cancellationToken);

        if (user is null ||
            !passwordHasher.Verify(password, user.PasswordHash))
        {
            return Result<AuthenticationTokens>.Failure(
                AuthenticationErrors.InvalidCredentials);
        }

        // Solo tras password válida: no filtrar inactivo como InvalidCredentials.
        // Código propio (distinto de PlatformAccessDenied) para que el front
        // muestre "cuenta inactiva" en vez del genérico de rol no admitido.
        if (!user.IsActive)
        {
            return Result<AuthenticationTokens>.Failure(
                AuthenticationErrors.UserInactive);
        }

        var identity = await BuildIdentityAsync(user, cancellationToken);
        var sessionStartedAt = ToUnspecifiedUtc(timeProvider.GetUtcNow());

        Result<AuthenticationTokens>? result = null;
        await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            result = await IssueTokensAsync(identity, sessionStartedAt, transactionToken);
        }, cancellationToken);

        return result!;
    }

    public async Task<Result<AuthenticationTokens>> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var tokenHash = refreshTokenProtector.Hash(refreshToken);

        var currentToken = await userTokenRepository.GetByTokenValueAsync(
            tokenHash, cancellationToken);

        if (currentToken is null || currentToken.IsExpiredAsOf(timeProvider))
        {
            return Result<AuthenticationTokens>.Failure(
                AuthenticationErrors.InvalidRefreshToken);
        }

        var now = timeProvider.GetUtcNow();
        var maxSession = TimeSpan.FromHours(jwtOptions.MaxSessionHours);
        if (now.UtcDateTime - currentToken.SessionStartedAt >= maxSession)
        {
            return Result<AuthenticationTokens>.Failure(
                AuthenticationErrors.InvalidRefreshToken);
        }

        var user = await usersRepository.GetByIdAsync(
            currentToken.UserId, cancellationToken);

        if (user is null)
        {
            return Result<AuthenticationTokens>.Failure(
                AuthenticationErrors.InvalidRefreshToken);
        }

        // Antes de rotar/borrar: usuario inactivo no renueva sesión.
        if (!user.IsActive)
        {
            return Result<AuthenticationTokens>.Failure(
                AuthenticationErrors.UserInactive);
        }

        var identity = await BuildIdentityAsync(user, cancellationToken);

        // Propagate the original login instant; never restart the session clock.
        var sessionStartedAt = currentToken.SessionStartedAt;

        Result<AuthenticationTokens>? result = null;
        await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            await userTokenRepository.DeleteAsync(currentToken, transactionToken);
            result = await IssueTokensAsync(identity, sessionStartedAt, transactionToken);
        }, cancellationToken);

        return result!;
    }

    public async Task<Result> RevokeAsync(
        Guid userId,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var tokenHash = refreshTokenProtector.Hash(refreshToken);

        var tokens = await userTokenRepository.GetAllByUserIdAsync(
            userId,
            cancellationToken);

        var token = tokens.FirstOrDefault(candidate =>
            candidate.TokenValue == tokenHash);

        if (token is null)
        {
            return Result.Failure(
                AuthenticationErrors.InvalidRefreshToken);
        }

        await userTokenRepository.DeleteAsync(
            token,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<CurrentProfile>> GetCurrentProfileAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await usersRepository.GetByIdAsync(
            userId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Result<CurrentProfile>.Failure(
                AuthenticationErrors.InvalidCredentials);
        }

        var identity = await BuildIdentityAsync(user, cancellationToken);

        return Result<CurrentProfile>.Success(CurrentProfile.From(identity));
    }

    private async Task<Result<AuthenticationTokens>> IssueTokensAsync(
        AuthenticatedIdentity identity,
        DateTime sessionStartedAt,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        var rawRefreshToken = refreshTokenProtector.Generate();

        var refreshTokenHash = refreshTokenProtector.Hash(
            rawRefreshToken);

        var refreshTokenExpiresAt =
            now.AddDays(jwtOptions.RefreshTokenDays);

        var userToken = new UserTokenEntity(
            identity.UserId,
            refreshTokenHash,
            RefreshTokenType,
            DateTime.SpecifyKind(
                refreshTokenExpiresAt.UtcDateTime,
                DateTimeKind.Unspecified),
            sessionStartedAt);

        await userTokenRepository.AddAsync(
            userToken,
            cancellationToken);

        var permissions = SystemRoles.IsSuperAdmin(identity.RoleId)
            ? Array.Empty<string>()
            : await sender.Send(
                new GetUserPermissionClaimsQuery(identity.RoleId),
                cancellationToken);

        var accessToken = jwtTokenIssuer.Issue(identity, permissions);

        return Result<AuthenticationTokens>.Success(
            new AuthenticationTokens(
                accessToken.Token,
                accessToken.ExpiresAt,
                rawRefreshToken,
                refreshTokenExpiresAt));
    }

    // U5: USERS ya trae contraseña y correo directamente -- ya no hace falta
    // resolver una cuenta ni credenciales separadas.
    private async Task<AuthenticatedIdentity> BuildIdentityAsync(
        UserEntity user,
        CancellationToken cancellationToken)
    {
        var role = await unitOfWork.RolesRepository.GetByIdAsync(
            user.RoleId, cancellationToken);

        return new AuthenticatedIdentity(
            user.Id,
            user.RoleId,
            role?.Name.Value ?? string.Empty,
            user.FullName,
            user.Email.Value,
            user.PhotoUrl.Value);
    }

    private static DateTime ToUnspecifiedUtc(DateTimeOffset instant) =>
        DateTime.SpecifyKind(instant.UtcDateTime, DateTimeKind.Unspecified);
}
