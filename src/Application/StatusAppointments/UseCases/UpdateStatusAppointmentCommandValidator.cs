using FluentValidation;

namespace Application.StatusAppointments.UseCases;

public sealed class UpdateStatusAppointmentCommandValidator
    : AbstractValidator<UpdateStatusAppointmentCommand>
{
    public UpdateStatusAppointmentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El ID del estado es obligatorio.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del estado es obligatorio.")
            .MaximumLength(50).WithMessage("El nombre no puede superar los 50 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(200).WithMessage("La descripción no puede superar los 200 caracteres.");
    }
}
