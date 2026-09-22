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
        if (request.Weight is <= 0)
        {
            throw new BadRequestException("El peso debe ser mayor a 0.");
        }

        if (request.Temperature is <= 0)
        {
            throw new BadRequestException("La temperatura debe ser mayor a 0.");
        }

        if (request.HeartRate is <= 0)
        {
            throw new BadRequestException("La frecuencia cardíaca debe ser mayor a 0.");
        }

        if (request.RespiratoryRate is <= 0)
        {
            throw new BadRequestException("La frecuencia respiratoria debe ser mayor a 0.");
        }

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
