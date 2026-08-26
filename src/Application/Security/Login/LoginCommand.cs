using Application.Common.Results;
using Application.Security.Models;
using MediatR;

namespace Application.Security.Login;

public sealed record LoginCommand(
    string Email,
    string Password) : IRequest<Result<AuthenticationTokens>>;