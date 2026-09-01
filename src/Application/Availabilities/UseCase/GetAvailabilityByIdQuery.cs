using Domain.Availabilities.Entities;
using MediatR;

namespace Application.Availabilities.UseCase;

public sealed record GetAvailabilityByIdQuery(Guid Id)
    : IRequest<Availability>;
