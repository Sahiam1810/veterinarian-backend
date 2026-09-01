using Domain.Diagnostics.Entities;
using MediatR;

namespace Application.Diagnostics.UseCases;

public sealed record UpdateDiagnosticCommand(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsActive) : IRequest<Diagnostic>;
