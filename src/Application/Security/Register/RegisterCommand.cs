using Application.Common.Results;
using Application.Security.Models;
using MediatR;

namespace Application.Security.Register;
public sealed record RegisterCommand(
    string FullName,
    string Email,
    string UserName,
    string Password) : IRequest<Result<AuthenticationTokens>>;