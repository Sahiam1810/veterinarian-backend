using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Appointments.Entities;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed class RecordAppointmentVitalsCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<RecordAppointmentVitalsCommand>
{
    public async Task Handle(
        RecordAppointmentVitalsCommand request,
        CancellationToken cancellationToken)
    {
        // Validación de "> 0" vive solo en RecordAppointmentVitalsCommandValidator
        // (ValidationBehavior ya corta antes de llegar acá) — no la repitas aquí.
        var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(
            request.AppointmentId,
            cancellationToken)
            ?? throw new NotFoundException("Cita médica no encontrada.");

        appointment.RecordVitals(
            request.Weight,
            request.Temperature,
            request.HeartRate,
            request.RespiratoryRate);

        await unitOfWork.AppointmentsRepository.UpdateAsync(
            appointment,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
