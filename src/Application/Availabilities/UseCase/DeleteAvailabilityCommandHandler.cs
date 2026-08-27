using Application.Common.Abstractions;
using MediatR;

namespace Application.Availabilities.UseCase;

public sealed class DeleteAvailabilityCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteAvailabilityCommand, bool>
{
    public async Task<bool> Handle(
        DeleteAvailabilityCommand request,
        CancellationToken cancellationToken)
    {
        var availability = await unitOfWork.AvailabilitiesRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (availability is null)
        {
            return false;
        }

        await unitOfWork.AvailabilitiesRepository.DeleteAsync(
            availability,
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
