using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Availabilities.UseCase;

public sealed class DeleteAvailabilityCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteAvailabilityCommand>
{
    public async Task Handle(
        DeleteAvailabilityCommand request,
        CancellationToken cancellationToken)
    {
        var availability = await unitOfWork.AvailabilitiesRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Disponibilidad no encontrada.");

        await unitOfWork.AvailabilitiesRepository.DeleteAsync(
            availability,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
