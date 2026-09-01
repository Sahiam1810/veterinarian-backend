using Domain.Diagnostics.Entities;
using MediatR;

namespace Application.Diagnostics.UseCases;

public sealed record GetDiagnosticByIdQuery(Guid Id) : IRequest<Diagnostic>;
