using Application.Common.Abstractions;
using MediatR;

namespace Application.StatusAppointments.UseCases;

public sealed class DeleteStatusAppointmentCommandHandler
    : IRequestHandler<DeleteStatusAppointmentCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public DeleteStatusAppointmentCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(
        DeleteStatusAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var statusAppointment = await _uow.StatusAppointmentsRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (statusAppointment is null)
            return false;

        await _uow.StatusAppointmentsRepository.DeleteAsync(
            statusAppointment,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
