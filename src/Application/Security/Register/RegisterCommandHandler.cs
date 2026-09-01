using Application.Common.Results;
using Application.Security.Models;
using Application.Security.Abstractions;
using MediatR; 

namespace Application.Security.Register;
public sealed class RegisterCommandHandler(IAuthenticationService authenticationService)
    : IRequestHandler<RegisterCommand, Result<AuthenticationTokens>>
{
    public Task<Result<AuthenticationTokens>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken) =>
        authenticationService.RegisterAsync(
            request.FullName,
            request.Email,
            request.UserName,
            request.Password,
            request.IdentificationNumber,
            cancellationToken);
}