using Application.Telegram.Abstractions;
using Application.Users.Abstraction;
using Infrastructure.Persistence;

namespace Infrastructure.Telegram;

public sealed class TelegramUnitOfWork(
    VeterinaryDbContext context,
    IUsersRepository usersRepository,
    ITelegramLinkCodeRepository linkCodesRepository,
    ITelegramUserLinkRepository userLinksRepository,
    ITelegramConversationLinkRepository conversationLinksRepository,
    ITelegramInboundUpdateRepository inboundUpdatesRepository)
    : ITelegramUnitOfWork
{
    public IUsersRepository UsersRepository { get; } = usersRepository;
    public ITelegramLinkCodeRepository LinkCodesRepository { get; } = linkCodesRepository;
    public ITelegramUserLinkRepository UserLinksRepository { get; } = userLinksRepository;
    public ITelegramConversationLinkRepository ConversationLinksRepository { get; } = conversationLinksRepository;
    public ITelegramInboundUpdateRepository InboundUpdatesRepository { get; } = inboundUpdatesRepository;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        if (context.Database.CurrentTransaction is not null)
        {
            await action(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await action(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
