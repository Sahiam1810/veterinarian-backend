using MediatR;

namespace Application.VeterinarianAbsences.UseCases;

public sealed record UpdateVeterinarianAbsenceCommand(
    Guid Id,
    DateTime StartAtUtc,
    DateTime EndAtUtc,
    string? Reason,
    bool IsFullDay) : IRequest;
