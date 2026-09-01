using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.StatusAppointments.UseCases;

public sealed class DeleteStatusAppointmentCommandHandler
    : IRequestHandler<DeleteStatusAppointmentCommand>
{
    private readonly IUnitOfWork _uow;

    public DeleteStatusAppointmentCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(
        DeleteStatusAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var statusAppointment = await _uow.StatusAppointmentsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Estado de cita no encontrado.");

        await _uow.StatusAppointmentsRepository.DeleteAsync(
            statusAppointment,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
    }
}
