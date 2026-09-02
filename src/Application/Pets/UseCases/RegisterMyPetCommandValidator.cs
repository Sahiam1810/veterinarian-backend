using Domain.Pets.ValueObjects;
using FluentValidation;

namespace Application.Pets.UseCases;

public sealed class RegisterMyPetCommandValidator : AbstractValidator<RegisterMyPetCommand>
{
    public RegisterMyPetCommandValidator()
    {
        RuleFor(command => command.UserAccountId).NotEmpty();
        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(PetName.MaxLength);
        RuleFor(command => command.Age)
            .InclusiveBetween(0, 150);
        RuleFor(command => command.Gender)
            .Must(gender =>
                string.Equals(gender, PetGender.Male, StringComparison.OrdinalIgnoreCase)
                || string.Equals(gender, PetGender.Female, StringComparison.OrdinalIgnoreCase))
            .WithMessage($"El género debe ser '{PetGender.Male}' o '{PetGender.Female}'.");
        RuleFor(command => command.Weight)
            .InclusiveBetween(PetWeight.Min, PetWeight.Max);
        RuleFor(command => command.Observations)
            .MaximumLength(PetObservations.MaxLength)
            .When(command => command.Observations is not null);
        RuleFor(command => command.SpeciesId).NotEmpty();
        RuleFor(command => command.RaceId).NotEmpty();
    }
}
