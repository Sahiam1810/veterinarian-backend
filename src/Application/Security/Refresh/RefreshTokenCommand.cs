using Application.Security.Models;
using Application.Common.Results;
using MediatR;


namespace Application.Security.Refresh;

public sealed record RefreshTokenCommand(
    string RefreshToken) : IRequest<Result<AuthenticationTokens>>;