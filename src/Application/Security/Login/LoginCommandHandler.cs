using Application.Common.Results;
using Application.Security.Abstractions;
using Application.Security.Models;
using MediatR;

namespace Application.Security.Login;

public sealed class LoginCommandHandler(IAuthenticationService authenticationService)
    : IRequestHandler<LoginCommand, Result<AuthenticationTokens>>
{
    public Task<Result<AuthenticationTokens>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken) =>
        authenticationService.LoginAsync(request.Email, request.Password, cancellationToken);
}