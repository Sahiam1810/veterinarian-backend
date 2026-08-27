using Domain.Availabilities.Entities;
using MediatR;

namespace Application.Availabilities.UseCase;

public sealed record GetAvailabilitiesByVeterinarianIdQuery(Guid VeterinarianId)
    : IRequest<IReadOnlyCollection<Availability>>;
