using Application.Telegram.Abstractions;
using Domain.Telegram.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Telegram.Repositories;

public sealed class TelegramUserLinkRepository(VeterinaryDbContext context)
    : ITelegramUserLinkRepository
{
    public Task<TelegramUserLink?> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken) =>
        context.Set<TelegramUserLink>().FirstOrDefaultAsync(link => link.PersonId == personId, cancellationToken);

    public Task<TelegramUserLink?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken) =>
        context.Set<TelegramUserLink>().FirstOrDefaultAsync(link => link.TelegramUserId == telegramUserId, cancellationToken);

    public Task<TelegramUserLink?> GetByTelegramChatIdAsync(long telegramChatId, CancellationToken cancellationToken) =>
        context.Set<TelegramUserLink>().FirstOrDefaultAsync(link => link.TelegramChatId == telegramChatId, cancellationToken);

    public async Task AddAsync(TelegramUserLink link, CancellationToken cancellationToken) =>
        await context.Set<TelegramUserLink>().AddAsync(link, cancellationToken);

    public Task UpdateAsync(TelegramUserLink link, CancellationToken cancellationToken)
    {
        context.Set<TelegramUserLink>().Update(link);
        return Task.CompletedTask;
    }
}
