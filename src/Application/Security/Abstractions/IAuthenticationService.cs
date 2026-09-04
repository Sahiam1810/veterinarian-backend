using Application.Common.Results;
using Application.Security.Models;


namespace Application.Security.Abstractions;
public interface IAuthenticationService
{
    Task<Result<AuthenticationTokens>> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<Result<AuthenticationTokens>> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken);

    Task<Result> RevokeAsync(
        Guid userId,
        string refreshToken,
        CancellationToken cancellationToken);

    Task<Result<CurrentProfile>> GetCurrentProfileAsync(
        Guid userAccountId,
        CancellationToken cancellationToken);
}