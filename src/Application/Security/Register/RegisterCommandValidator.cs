using FluentValidation;

namespace Application.Security.Register;
public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(150);
        RuleFor(command => command.UserName)
            .NotEmpty()
            .MaximumLength(50);
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(120);
        RuleFor(command => command.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(100);
    }
}