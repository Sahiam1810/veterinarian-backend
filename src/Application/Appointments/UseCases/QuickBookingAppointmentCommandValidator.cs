using Application.Clients.Errors;
using Domain.Clients.ValueObjects;
using Domain.Pets.ValueObjects;
using FluentValidation;

namespace Application.Appointments.UseCases;

public sealed class QuickBookingAppointmentCommandValidator : AbstractValidator<QuickBookingAppointmentCommand>
{
    public QuickBookingAppointmentCommandValidator()
    {
        RuleFor(command => command.PetName)
            .NotEmpty().WithMessage("El nombre de la mascota es obligatorio.")
            .MaximumLength(PetName.MaxLength).WithMessage($"El nombre no puede superar los {PetName.MaxLength} caracteres.");

        RuleFor(command => command.SpeciesId)
            .NotEmpty().WithMessage("El ID de la especie es obligatorio.");

        RuleFor(command => command.ServiceId)
            .NotEmpty().WithMessage("El ID del servicio es obligatorio.");

        RuleFor(command => command.VeterinarianId)
            .NotEmpty().WithMessage("El ID del veterinario es obligatorio.");

        RuleFor(command => command.ScheduledStart)
            .NotEmpty().WithMessage("La fecha y hora de inicio es obligatoria.");

        When(command => !command.ClientId.HasValue || command.ClientId.Value == Guid.Empty, () =>
        {
            RuleFor(command => command.ClientPhoneNumber)
                .NotEmpty().WithErrorCode(ClientErrorCodes.PhoneRequired).WithMessage("El número de teléfono del cliente es obligatorio.");

            When(command => !string.IsNullOrWhiteSpace(command.ClientPhoneNumber), () =>
            {
                RuleFor(command => command.ClientPhoneNumber)
                    .Must(val => ClientPhoneNumber.TryCreate(val, out _))
                    .WithErrorCode(ClientErrorCodes.PhoneInvalidFormat)
                    .WithMessage("El formato del número de teléfono no es válido.");
            });

            RuleFor(command => command.ClientFullName)
                .NotEmpty().WithMessage("El nombre completo del cliente es obligatorio.")
                .MaximumLength(ClientFullName.MaxLength)
                .WithMessage($"El nombre completo no puede superar los {ClientFullName.MaxLength} caracteres.");
        });
    }
}
