using Application.Common.Results;
using MediatR;

namespace Application.Security.EmailOtp;

public sealed record RequestEmailOtpCommand(string Email) : IRequest<Result>;
