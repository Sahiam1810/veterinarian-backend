using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.StatusAppointments.UseCases;

public sealed class UpdateStatusAppointmentCommandHandler
    : IRequestHandler<UpdateStatusAppointmentCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public UpdateStatusAppointmentCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(
        UpdateStatusAppointmentCommand request,
        CancellationToken cancellationToken)
    {
        var statusAppointment = await _uow.StatusAppointmentsRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (statusAppointment is null)
            return false;

        var nameExists = await _uow.StatusAppointmentsRepository.ExistsByNameAsync(
            request.Name,
            cancellationToken,
            excludedId: request.Id);

        if (nameExists)
        {
            throw new ConflictException(
                "Ya existe otro estado de cita con ese nombre.");
        }

        statusAppointment.Update(
            request.Name,
            request.Description);

        await _uow.StatusAppointmentsRepository.UpdateAsync(
            statusAppointment,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
