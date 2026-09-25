using Domain.Pets.ValueObjects;
using FluentValidation;

namespace Application.Pets.UseCases;

public sealed class CreatePetCommandValidator : AbstractValidator<CreatePetCommand>
{
    public CreatePetCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .WithMessage("El nombre de la mascota es obligatorio.")
            .MaximumLength(PetName.MaxLength)
            .WithMessage($"El nombre no puede superar los {PetName.MaxLength} caracteres.");

        RuleFor(command => command.Age)
            .GreaterThanOrEqualTo(0)
            .WithMessage("La edad no puede ser negativa.");

        RuleFor(command => command.Gender)
            .NotEmpty()
            .WithMessage("El género de la mascota es obligatorio.")
            .Must(gender =>
                string.Equals(gender, PetGender.Male, StringComparison.OrdinalIgnoreCase)
                || string.Equals(gender, PetGender.Female, StringComparison.OrdinalIgnoreCase))
            .WithMessage($"El género debe ser '{PetGender.Male}' (macho) o '{PetGender.Female}' (hembra).");

        RuleFor(command => command.Weight)
            .InclusiveBetween(PetWeight.Min, PetWeight.Max)
            .WithMessage($"El peso debe estar entre {PetWeight.Min} y {PetWeight.Max} kg.");

        RuleFor(command => command.Observations)
            .MaximumLength(PetObservations.MaxLength)
            .WithMessage(
                $"Las observaciones no pueden superar los {PetObservations.MaxLength} caracteres.");

        RuleFor(command => command.PhotoUrl)
            .MaximumLength(PetPhotoUrl.MaxLength)
            .WithMessage($"La URL de la foto no puede superar los {PetPhotoUrl.MaxLength} caracteres.")
            .Must(url =>
                string.IsNullOrWhiteSpace(url)
                || (Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            .WithMessage("La URL de la foto debe ser una dirección http o https válida.");

        RuleFor(command => command.SpeciesId)
            .NotEmpty()
            .WithMessage("La especie es obligatoria.");

        RuleFor(command => command.RaceId)
            .NotEmpty()
            .WithMessage("La raza es obligatoria.");
    }
}
