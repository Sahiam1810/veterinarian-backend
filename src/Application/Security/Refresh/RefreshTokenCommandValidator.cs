using FluentValidation;

namespace Application.Security.Refresh;
public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(command => command.RefreshToken).NotEmpty();
    }
}