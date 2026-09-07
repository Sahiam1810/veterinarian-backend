using Application.Common.Results;
using Application.Security.Models;
using MediatR;

namespace Application.Security.EmailOtp;

public sealed record ConfirmEmailOtpCommand(
    string Email,
    string Code) : IRequest<Result<AuthenticationTokens>>;
