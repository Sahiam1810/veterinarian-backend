namespace Application.Security.Profile;
using MediatR;
using Application.Security.Models;
using Application.Common.Results;
public sealed record GetCurrentProfileQuery(Guid UserAccountId)
    : IRequest<Result<CurrentProfile>>;