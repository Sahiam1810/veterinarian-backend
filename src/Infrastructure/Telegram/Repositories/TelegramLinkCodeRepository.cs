using Application.Telegram.Abstractions;
using Domain.Telegram.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Telegram.Repositories;

public sealed class TelegramLinkCodeRepository(VeterinaryDbContext context)
    : ITelegramLinkCodeRepository
{
    public Task<TelegramLinkCode?> GetActiveByHashAsync(string codeHash, DateTime now, CancellationToken cancellationToken) =>
        context.Set<TelegramLinkCode>().FirstOrDefaultAsync(code =>
            code.CodeHash == codeHash && code.ConsumedAt == null &&
            code.InvalidatedAt == null && code.ExpiresAt > now, cancellationToken);

    public async Task<IReadOnlyCollection<TelegramLinkCode>> GetPendingByPersonIdAsync(
        Guid personId, DateTime now, CancellationToken cancellationToken) =>
        await context.Set<TelegramLinkCode>().Where(code =>
            code.PersonId == personId && code.ConsumedAt == null &&
            code.InvalidatedAt == null && code.ExpiresAt > now).ToListAsync(cancellationToken);

    public async Task AddAsync(TelegramLinkCode code, CancellationToken cancellationToken) =>
        await context.Set<TelegramLinkCode>().AddAsync(code, cancellationToken);

    public Task UpdateAsync(TelegramLinkCode code, CancellationToken cancellationToken)
    {
        context.Set<TelegramLinkCode>().Update(code);
        return Task.CompletedTask;
    }
}
