using Application.Diagnostics.Abstraction;
using Domain.Diagnostics.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Diagnostics.Repositories;

public class DiagnosticRepository : IDiagnosticRepository
{
    private readonly VeterinaryDbContext _context;

    public DiagnosticRepository(VeterinaryDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyCollection<Diagnostic>> GetAllAsync(bool onlyActive = true, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Diagnostic>()
            .Where(x => !onlyActive || x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Diagnostic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Diagnostic>()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Diagnostic?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpper();
        return await _context.Set<Diagnostic>()
            .FirstOrDefaultAsync(x => x.Code == normalizedCode, cancellationToken);
    }

    public async Task AddAsync(Diagnostic diagnostic, CancellationToken cancellationToken = default)
    {
        await _context.Set<Diagnostic>().AddAsync(diagnostic, cancellationToken);
    }

    public Task UpdateAsync(Diagnostic diagnostic, CancellationToken cancellationToken = default)
    {
        _context.Set<Diagnostic>().Update(diagnostic);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Diagnostic diagnostic, CancellationToken cancellationToken = default)
    {
        _context.Set<Diagnostic>().Remove(diagnostic);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpper();
        return await _context.Set<Diagnostic>()
            .AnyAsync(x => x.Code == normalizedCode, cancellationToken);
    }

    public async Task<bool> ExistsCodeForDifferentIdAsync(Guid id, string code, CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpper();
        return await _context.Set<Diagnostic>()
            .AnyAsync(x => x.Id != id && x.Code == normalizedCode, cancellationToken);
    }
}
