using Domain.Species.ValueObjects;
using FluentValidation;

namespace Application.Species.UseCases;

public sealed class CreateSpeciesCommandValidator : AbstractValidator<CreateSpeciesCommand>
{
    public CreateSpeciesCommandValidator()
    {
        RuleFor(command => command.Name)
            .NotEmpty()
            .WithMessage("El nombre de la especie es obligatorio.")
            .MaximumLength(SpeciesName.MaxLength)
            .WithMessage(
                $"El nombre de la especie no puede superar los {SpeciesName.MaxLength} caracteres.");
    }
}
