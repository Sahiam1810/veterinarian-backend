using Application.Common.Results;
using Application.Security.EmailOtp;
using Application.Security.Errors;
using Application.Security.Models;

namespace Infrastructure.Security.Authentication;

public sealed class EmailOtpAuthenticationStub : IEmailOtpAuthenticationService
{
    public Task<Result> RequestOtpAsync(
        string email,
        CancellationToken cancellationToken) =>
        Task.FromResult(Result.Success());

    public Task<Result<AuthenticationTokens>> ConfirmOtpAsync(
        string email,
        string code,
        CancellationToken cancellationToken) =>
        Task.FromResult(Result<AuthenticationTokens>.Failure(AuthenticationErrors.InvalidOtp));
}
