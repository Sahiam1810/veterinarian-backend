using FluentValidation;

namespace Application.UserTokens.UseCase;

public sealed class CreateUserTokenCommandValidator
    : AbstractValidator<CreateUserTokenCommand>
{
    // "refresh"/"access" solo pueden originarse del flujo real de login/refresh
    // (AuthenticationService.IssueTokensAsync, que genera el valor y lo hashea).
    // Permitir crearlos a mano acá dejaba forjar un refresh token válido para
    // cualquier cuenta: se elige un secreto, se manda su SHA-256 como TokenValue,
    // y ese secreto ya sirve como RefreshToken real en POST /api/auth/refresh.
    private static readonly string[] ReservedTokenTypes = ["refresh", "access"];

    public CreateUserTokenCommandValidator()
    {
        RuleFor(command => command.AccountId)
            .NotEmpty()
            .WithMessage("Debe asociar el token a una cuenta.");

        RuleFor(command => command.TokenValue)
            .NotEmpty()
            .WithMessage("El valor del token es obligatorio.")
            .MaximumLength(500)
            .WithMessage("El valor del token no puede superar los 500 caracteres.");

        RuleFor(command => command.TokenType)
            .NotEmpty()
            .WithMessage("El tipo de token es obligatorio.")
            .MaximumLength(20)
            .WithMessage("El tipo de token no puede superar los 20 caracteres.")
            .Must(tokenType => tokenType is null
                || !ReservedTokenTypes.Contains(tokenType.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage(
                "No se pueden crear tokens de tipo 'refresh' ni 'access' manualmente: "
                + "solo pueden originarse del flujo real de login/refresh.");

        RuleFor(command => command.ExpiresAt)
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("La fecha de expiración debe ser futura.");
    }
}
