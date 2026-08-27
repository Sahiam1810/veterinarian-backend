using Domain.Diagnostics.Entities;
using MediatR;

namespace Application.Diagnostics.UseCases;

public sealed record GetAllDiagnosticsQuery(bool OnlyActive = true)
    : IRequest<IReadOnlyCollection<Diagnostic>>;
