using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Availabilities.Entities;
using MediatR;

namespace Application.Availabilities.UseCase;

public sealed class GetAvailabilityByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAvailabilityByIdQuery, Availability>
{
    public async Task<Availability> Handle(
        GetAvailabilityByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await unitOfWork.AvailabilitiesRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Disponibilidad no encontrada.");
    }
}
