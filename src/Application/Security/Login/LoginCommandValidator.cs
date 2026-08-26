using FluentValidation;

namespace Application.Security.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(120);
        RuleFor(command => command.Password).NotEmpty();
    }
}