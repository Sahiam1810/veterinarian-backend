using Application.Common.Results;
using Application.Security.Models;
using Application.Security.Abstractions;
using MediatR;

namespace Application.Security.Refresh;

public sealed class RefreshTokenCommandHandler(IAuthenticationService authenticationService)
    : IRequestHandler<RefreshTokenCommand, Result<AuthenticationTokens>>
{
    public Task<Result<AuthenticationTokens>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken) =>
        authenticationService.RefreshAsync(request.RefreshToken, cancellationToken);
}