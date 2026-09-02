using Application.Users.Abstraction;

namespace Application.Telegram.Abstractions;

public interface ITelegramUnitOfWork
{
    IUsersRepository UsersRepository { get; }

    ITelegramLinkCodeRepository LinkCodesRepository { get; }

    ITelegramUserLinkRepository UserLinksRepository { get; }

    ITelegramConversationLinkRepository ConversationLinksRepository { get; }

    ITelegramInboundUpdateRepository InboundUpdatesRepository { get; }

    ITelegramLinkingSessionRepository LinkingSessionsRepository { get; }

    ITelegramRegistrationSessionRepository RegistrationSessionsRepository { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default);
}
