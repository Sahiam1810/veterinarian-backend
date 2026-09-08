using MediatR;

namespace Application.VeterinarianAbsences.UseCases;

public sealed record CreateVeterinarianAbsenceCommand(
    Guid VeterinarianId,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    string? Reason,
    bool IsFullDay) : IRequest<Guid>;
