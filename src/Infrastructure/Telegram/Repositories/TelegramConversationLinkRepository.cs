using Application.Telegram.Abstractions;
using Domain.Telegram.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Telegram.Repositories;

public sealed class TelegramConversationLinkRepository(VeterinaryDbContext context)
    : ITelegramConversationLinkRepository
{
    public Task<TelegramConversationBinding?> GetBindingAsync(
        Guid telegramUserLinkId,
        CancellationToken cancellationToken) =>
        (from link in context.Set<TelegramConversationLink>().AsNoTracking()
         join conversation in context.Set<Domain.ChatConversations.Entities.ChatConversation>().AsNoTracking()
             on link.ConversationId equals conversation.Id
         where link.TelegramUserLinkId == telegramUserLinkId
         select new TelegramConversationBinding(conversation.Id, conversation.Closed))
        .FirstOrDefaultAsync(cancellationToken);

    public Task<TelegramConversationLink?> GetByUserLinkIdAsync(Guid telegramUserLinkId, CancellationToken cancellationToken) =>
        context.Set<TelegramConversationLink>().FirstOrDefaultAsync(
            link => link.TelegramUserLinkId == telegramUserLinkId, cancellationToken);

    public async Task AddAsync(TelegramConversationLink link, CancellationToken cancellationToken) =>
        await context.Set<TelegramConversationLink>().AddAsync(link, cancellationToken);

    public Task UpdateAsync(TelegramConversationLink link, CancellationToken cancellationToken)
    {
        context.Set<TelegramConversationLink>().Update(link);
        return Task.CompletedTask;
    }
}
