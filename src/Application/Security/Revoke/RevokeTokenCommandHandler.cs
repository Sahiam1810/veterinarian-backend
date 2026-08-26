using Application.Common.Results;
using Application.Security.Abstractions;
using MediatR;


namespace Application.Security.Revoke;

public sealed class RevokeTokenCommandHandler(IAuthenticationService authenticationService)
    : IRequestHandler<RevokeTokenCommand, Result>
{
    public Task<Result> Handle(
        RevokeTokenCommand request,
        CancellationToken cancellationToken) =>
        authenticationService.RevokeAsync(request.UserId, request.RefreshToken, cancellationToken);
}