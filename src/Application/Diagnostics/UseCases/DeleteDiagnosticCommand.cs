using MediatR;

namespace Application.Diagnostics.UseCases;

public sealed record DeleteDiagnosticCommand(Guid Id) : IRequest;
