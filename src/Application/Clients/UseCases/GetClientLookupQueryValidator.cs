using FluentValidation;

namespace Application.Clients.UseCases;

public class GetClientLookupQueryValidator : AbstractValidator<GetClientLookupQuery>
{
    public GetClientLookupQueryValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.IdentificationNumber) || !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("Debe proporcionar al menos un parámetro de búsqueda ('identification' o 'phone').");
    }
}
