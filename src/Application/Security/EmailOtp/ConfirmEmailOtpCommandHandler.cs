using Application.Common.Results;
using Application.Security.Models;
using MediatR;

namespace Application.Security.EmailOtp;

public sealed class ConfirmEmailOtpCommandHandler(IEmailOtpAuthenticationService emailOtpAuthService)
    : IRequestHandler<ConfirmEmailOtpCommand, Result<AuthenticationTokens>>
{
    public Task<Result<AuthenticationTokens>> Handle(
        ConfirmEmailOtpCommand request,
        CancellationToken cancellationToken) =>
        emailOtpAuthService.ConfirmOtpAsync(request.Email, request.Code, cancellationToken);
}
