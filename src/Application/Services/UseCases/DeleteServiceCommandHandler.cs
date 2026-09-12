using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Services.UseCases;

// Borra servicio solo si está inactivo y sin citas asociadas.
public sealed class DeleteServiceCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteServiceCommand>
{
    public async Task Handle(
        DeleteServiceCommand request,
        CancellationToken cancellationToken)
    {
        var service = await unitOfWork.ServicesRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Servicio no encontrado.");

        if (service.IsActive)
        {
            throw new ConflictException(
                "Desactiva el servicio antes de eliminarlo. Las citas existentes se conservan; no se podrá asignar a citas nuevas.");
        }

        var hasAppointments = await unitOfWork.AppointmentsRepository.ExistsByServiceIdAsync(
            service.Id,
            cancellationToken);
        if (hasAppointments)
        {
            throw new ConflictException(
                "El servicio tiene citas asociadas. Déjalo inactivo; no se puede eliminar mientras existan citas.");
        }

        await unitOfWork.ServicesRepository.DeleteAsync(
            service,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
