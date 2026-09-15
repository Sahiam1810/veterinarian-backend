using Domain.Clients.ValueObjects;
using FluentValidation;

namespace Application.Clients.UseCases;

// Al menos un criterio; valida formato de cédula/teléfono antes del repositorio (evita ArgumentException).
public class GetClientLookupQueryValidator : AbstractValidator<GetClientLookupQuery>
{
    public GetClientLookupQueryValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.IdentificationNumber) || !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("Debe proporcionar al menos un parámetro de búsqueda ('identification' o 'phone').");

        When(x => !string.IsNullOrWhiteSpace(x.IdentificationNumber), () =>
        {
            RuleFor(x => x.IdentificationNumber!)
                .MaximumLength(ClientIdentificationNumber.MaxLength)
                .WithMessage(
                    $"El número de identificación no puede superar los {ClientIdentificationNumber.MaxLength} caracteres.")
                .Must(BeValidIdentification)
                .WithMessage("El número de identificación no es válido.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber), () =>
        {
            RuleFor(x => x.PhoneNumber!)
                .Must(BeValidPhone)
                .WithMessage(
                    $"El teléfono debe tener entre 7 y {ClientPhoneNumber.MaxLength} dígitos.");
        });
    }

    private static bool BeValidIdentification(string value)
    {
        try
        {
            _ = ClientIdentificationNumber.Create(value);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool BeValidPhone(string value)
    {
        try
        {
            _ = ClientPhoneNumber.Create(value);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
