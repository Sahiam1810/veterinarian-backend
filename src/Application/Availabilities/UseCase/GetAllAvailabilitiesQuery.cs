using Domain.Availabilities.Entities;
using MediatR;

namespace Application.Availabilities.UseCase;

public sealed record GetAllAvailabilitiesQuery
    : IRequest<IReadOnlyCollection<Availability>>;
