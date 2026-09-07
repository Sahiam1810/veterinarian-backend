using FluentValidation;

namespace Application.Security.EmailOtp;

public sealed class RequestEmailOtpCommandValidator : AbstractValidator<RequestEmailOtpCommand>
{
    public RequestEmailOtpCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(120);
    }
}
