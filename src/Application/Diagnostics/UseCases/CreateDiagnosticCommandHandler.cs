using Application.Common.Abstractions;
using Domain.Diagnostics.Entities;
using MediatR;

namespace Application.Diagnostics.UseCases;

public sealed class CreateDiagnosticCommandHandler
    : IRequestHandler<CreateDiagnosticCommand, Diagnostic>
{
    private readonly IUnitOfWork _uow;

    public CreateDiagnosticCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Diagnostic> Handle(
        CreateDiagnosticCommand request,
        CancellationToken cancellationToken)
    {
        var diagnostic = new Diagnostic
        {
            Id = Guid.NewGuid(),
            Code = request.Code.Trim().ToUpper(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.DiagnosticsRepository.AddAsync(diagnostic, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return diagnostic;
    }
}
