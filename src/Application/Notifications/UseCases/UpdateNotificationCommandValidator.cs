using Domain.Notifications.ValueObjects;
using FluentValidation;

namespace Application.Notifications.UseCases;

public sealed class UpdateNotificationCommandValidator
    : AbstractValidator<UpdateNotificationCommand>
{
    public UpdateNotificationCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El id de la notificación es obligatorio.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("El id de usuario es obligatorio.");

        RuleFor(x => x.AppointmentId)
            .NotEmpty().WithMessage("El id de la cita es obligatorio.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("El mensaje es obligatorio.")
            .MaximumLength(NotificationMessage.MaxLength)
            .WithMessage($"El mensaje no puede superar los {NotificationMessage.MaxLength} caracteres.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("El estado es obligatorio.")
            .MaximumLength(NotificationStatus.MaxLength)
            .WithMessage($"El estado no puede superar los {NotificationStatus.MaxLength} caracteres.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("El tipo es obligatorio.")
            .MaximumLength(NotificationType.MaxLength)
            .WithMessage($"El tipo no puede superar los {NotificationType.MaxLength} caracteres.");
    }
}
