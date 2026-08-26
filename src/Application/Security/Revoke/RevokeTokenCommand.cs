using Application.Common.Results;
using MediatR;

namespace Application.Security.Revoke;
public sealed record RevokeTokenCommand(
    Guid UserId,
    string RefreshToken) : IRequest<Result>;