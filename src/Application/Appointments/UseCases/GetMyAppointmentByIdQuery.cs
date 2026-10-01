using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Appointments.Entities;
using MediatR;

namespace Application.Appointments.UseCases;

public sealed record GetMyAppointmentByIdQuery(Guid AppointmentId, Guid ClientId)
    : IRequest<Appointment>;

public sealed class GetMyAppointmentByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetMyAppointmentByIdQuery, Appointment>
{
    public async Task<Appointment> Handle(
        GetMyAppointmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var client = await unitOfWork.ClientsRepository.GetByIdAsync(
            request.ClientId,
            cancellationToken)
            ?? throw new NotFoundException("Cliente no encontrado.");

        var clientPets = await unitOfWork.ClientPetsRepository.GetByClientIdAsync(
            client.Id,
            cancellationToken);
        var appointment = await unitOfWork.AppointmentsRepository.GetByIdAsync(
            request.AppointmentId,
            cancellationToken);

        if (appointment is null || clientPets.All(item => item.Id != appointment.ClientPetId))
        {
            throw new NotFoundException("Cita médica no encontrada.");
        }

        return appointment;
    }
}
