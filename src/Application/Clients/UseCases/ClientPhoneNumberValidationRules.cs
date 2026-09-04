using Application.Clients.Errors;
using Domain.Clients.ValueObjects;
using FluentValidation;

namespace Application.Clients.UseCases;

internal static class ClientPhoneNumberValidationRules
{
    public static IRuleBuilderOptions<T, string?> RequiredPhoneNumber<T>(
        this IRuleBuilderInitial<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithErrorCode(ClientErrorCodes.PhoneRequired)
            .WithMessage("El teléfono es obligatorio.")
            .Must(value => ClientPhoneNumber.TryCreate(value, out _))
            .WithErrorCode(ClientErrorCodes.PhoneInvalidFormat)
            .WithMessage(
                $"El teléfono debe tener entre {ClientPhoneNumber.MinLength} y {ClientPhoneNumber.MaxLength} dígitos.");
    }
}
