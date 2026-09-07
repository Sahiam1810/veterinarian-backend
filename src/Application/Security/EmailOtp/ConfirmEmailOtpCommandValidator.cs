using FluentValidation;

namespace Application.Security.EmailOtp;

public sealed class ConfirmEmailOtpCommandValidator : AbstractValidator<ConfirmEmailOtpCommand>
{
    public ConfirmEmailOtpCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(120);

        RuleFor(command => command.Code)
            .NotEmpty()
            .Length(6);
    }
}
