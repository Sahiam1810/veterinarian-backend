using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Appointments.Entities;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed class GetAppointmentReceiptQueryHandler(IUnitOfWork unitOfWork) 
    : IRequestHandler<GetAppointmentReceiptQuery, AppointmentReceiptResult>
{
    public async Task<AppointmentReceiptResult> Handle(GetAppointmentReceiptQuery request, CancellationToken cancellationToken)
    {
        var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(request.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("Cita médica no encontrada.");

        var client = appointment.ClientPet is not null
            ? await unitOfWork.ClientsRepository.GetByIdAsync(appointment.ClientPet.ClientId, cancellationToken)
            : null;

        var petName = appointment.ClientPet?.Pet?.Name?.Value ?? "Paciente Desconocido";
        var ownerName = client?.FullName?.Value ?? "Dueño Desconocido";
        var ownerPhone = client?.PhoneNumber?.Value;
        var serviceName = appointment.Service?.Name ?? "Servicio Desconocido";
        var servicePrice = appointment.IsPaid
            ? appointment.PaidAmount ?? appointment.Service?.Price ?? 0m
            : appointment.Service?.Price ?? 0m;

        return new AppointmentReceiptResult(
            petName,
            ownerName,
            ownerPhone,
            serviceName,
            servicePrice,
            appointment.ScheduledStart,
            appointment.IsPaid
        );
    }
}
