using MediatR;

namespace Application.Availabilities.UseCase;

public sealed record DeleteAvailabilityCommand(Guid Id) : IRequest<bool>;
