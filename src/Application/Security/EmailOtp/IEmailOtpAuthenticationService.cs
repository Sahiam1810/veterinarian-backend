using Application.Common.Results;
using Application.Security.Models;

namespace Application.Security.EmailOtp;

public interface IEmailOtpAuthenticationService
{
    Task<Result> RequestOtpAsync(
        string email,
        CancellationToken cancellationToken);

    Task<Result<AuthenticationTokens>> ConfirmOtpAsync(
        string email,
        string code,
        CancellationToken cancellationToken);
}
