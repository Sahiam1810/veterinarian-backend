using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.StatusAppointments.Entities;
using MediatR;

namespace Application.StatusAppointments.UseCases;

public sealed class CreateStatusAppointmentCommandHandler
    : IRequestHandler<CreateStatusAppointmentCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateStatusAppointmentCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(
        CreateStatusAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var exists = await _uow.StatusAppointmentsRepository.ExistsByNameAsync(
            request.Name,
            cancellationToken);

        if (exists)
        {
            throw new ConflictException(
                "Ya existe un estado de cita con ese nombre.");
        }

        var statusAppointment = new StatusAppointment(
            request.Name,
            request.Description);

        await _uow.StatusAppointmentsRepository.AddAsync(
            statusAppointment,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return statusAppointment.Id;
    }
}
