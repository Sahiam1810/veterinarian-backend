using MediatR;

namespace Application.Veterinarians.UseCases;

public sealed record UpdateVeterinarianCommand(
    Guid Id,
    Guid UserId,
    Guid SpecialtyId,
    string LicenseNumber) : IRequest<bool>;
