using Domain.Pets.ValueObjects;
using FluentValidation;

namespace Application.Pets.UseCases;

public sealed class UpdateMyPetProfileCommandValidator : AbstractValidator<UpdateMyPetProfileCommand>
{
    public UpdateMyPetProfileCommandValidator()
    {
        RuleFor(x => x.UserAccountId).NotEmpty();
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.ExpectedUpdatedAt).NotEmpty();
        RuleFor(x => x).Must(HasChanges).WithMessage("Debe indicar al menos un cambio.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(PetName.MaxLength)
            .When(x => x.Name is not null);
        RuleFor(x => x.Age).GreaterThanOrEqualTo(0).When(x => x.Age.HasValue);
        RuleFor(x => x.Gender)
            .Must(value => string.Equals(value, PetGender.Male, StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, PetGender.Female, StringComparison.OrdinalIgnoreCase))
            .When(x => x.Gender is not null)
            .WithMessage($"El género debe ser '{PetGender.Male}' o '{PetGender.Female}'.");
        RuleFor(x => x.Weight).InclusiveBetween(PetWeight.Min, PetWeight.Max)
            .When(x => x.Weight.HasValue);
        RuleFor(x => x.Observations).MaximumLength(PetObservations.MaxLength)
            .When(x => x.ChangeObservations && x.Observations is not null);
        RuleFor(x => x.SpeciesId).NotEmpty().When(x => x.SpeciesId.HasValue);
        RuleFor(x => x.RaceId).NotEmpty().When(x => x.RaceId.HasValue);
    }

    private static bool HasChanges(UpdateMyPetProfileCommand command) =>
        command.Name is not null || command.Age.HasValue || command.Gender is not null
        || command.Weight.HasValue || command.ChangeObservations
        || command.SpeciesId.HasValue || command.RaceId.HasValue;
}
