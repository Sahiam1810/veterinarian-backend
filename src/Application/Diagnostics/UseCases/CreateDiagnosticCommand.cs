using Domain.Diagnostics.Entities;
using MediatR;

namespace Application.Diagnostics.UseCases;

public sealed record CreateDiagnosticCommand(
    string Code,
    string Name,
    string? Description) : IRequest<Diagnostic>;
