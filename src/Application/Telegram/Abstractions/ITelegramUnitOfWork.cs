using Application.Clients.Abstraction;
using Application.UserAccounts.Abstraction;
using Application.Users.Abstraction;

namespace Application.Telegram.Abstractions;

public interface ITelegramUnitOfWork
{
    IClientRepository ClientsRepository { get; }

    IUsersRepository UsersRepository { get; }

    // Necesario para crear la cuenta fantasma (sin password) al vincular un
    // registro hecho desde el bot (ver LinkTelegramBotAccountCommand).
    IUserAccountsRepository UserAccountsRepository { get; }

    ITelegramUserLinkRepository UserLinksRepository { get; }

    ITelegramConversationLinkRepository ConversationLinksRepository { get; }

    ITelegramInboundUpdateRepository InboundUpdatesRepository { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default);
}
