using MediatR;

namespace Application.Veterinarians.UseCases;

public sealed record CreateVeterinarianCommand(
    Guid UserId,
    Guid SpecialtyId,
    string LicenseNumber) : IRequest<Guid>;
