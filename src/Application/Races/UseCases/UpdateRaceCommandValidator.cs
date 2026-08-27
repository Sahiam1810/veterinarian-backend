using Domain.Races.ValueObjects;
using FluentValidation;

namespace Application.Races.UseCases;

public sealed class UpdateRaceCommandValidator : AbstractValidator<UpdateRaceCommand>
{
    public UpdateRaceCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .WithMessage("El nombre de la raza es obligatorio.")
            .MaximumLength(RaceName.MaxLength)
            .WithMessage(
                $"El nombre de la raza no puede superar los {RaceName.MaxLength} caracteres.");
    }
}
