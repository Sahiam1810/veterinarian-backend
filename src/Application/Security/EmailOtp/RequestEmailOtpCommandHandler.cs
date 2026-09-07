using Application.Common.Results;
using MediatR;

namespace Application.Security.EmailOtp;

public sealed class RequestEmailOtpCommandHandler(IEmailOtpAuthenticationService emailOtpAuthService)
    : IRequestHandler<RequestEmailOtpCommand, Result>
{
    public Task<Result> Handle(
        RequestEmailOtpCommand request,
        CancellationToken cancellationToken) =>
        emailOtpAuthService.RequestOtpAsync(request.Email, cancellationToken);
}
