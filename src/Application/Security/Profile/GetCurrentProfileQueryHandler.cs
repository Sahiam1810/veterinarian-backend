using MediatR;
using Application.Security.Models;
using Application.Common.Results;
using Application.Security.Abstractions;

namespace Application.Security.Profile;

public sealed class GetCurrentProfileQueryHandler(
    IAuthenticationService authenticationService)
    : IRequestHandler<GetCurrentProfileQuery, Result<CurrentProfile>>
{
    public Task<Result<CurrentProfile>> Handle(
        GetCurrentProfileQuery request,
        CancellationToken cancellationToken) =>
        authenticationService.GetCurrentProfileAsync(
            request.UserAccountId,
            cancellationToken);
}