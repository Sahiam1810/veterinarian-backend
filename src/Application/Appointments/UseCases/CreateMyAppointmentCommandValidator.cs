using FluentValidation;

namespace Application.Appointments.UseCases;

public sealed class CreateMyAppointmentCommandValidator
    : AbstractValidator<CreateMyAppointmentCommand>
{
    public CreateMyAppointmentCommandValidator()
    {
        RuleFor(command => command.UserAccountId).NotEmpty();
        RuleFor(command => command.PetId).NotEmpty();
        RuleFor(command => command.VeterinarianId).NotEmpty();
        RuleFor(command => command.ServiceId).NotEmpty();
        RuleFor(command => command.ScheduledStartUtc)
            .Must(value => value.Kind == DateTimeKind.Utc)
            .WithMessage("La fecha de inicio debe estar expresada en UTC.");
        RuleFor(command => command.Notes).MaximumLength(100);
        RuleFor(command => command.RequesterPhoneNumber)
            .Must(phone =>
                string.IsNullOrWhiteSpace(phone)
                || Domain.Appointments.ValueObjects.RequesterPhoneNumber.Normalize(phone).Length
                    is >= 7 and <= Domain.Appointments.ValueObjects.RequesterPhoneNumber.MaxLength)
            .WithMessage("El teléfono del solicitante no es válido.");
        RuleFor(command => command.IdempotencyKey).NotEmpty().MaximumLength(160);
    }
}
