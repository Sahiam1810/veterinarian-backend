using Application.Clients.Abstraction;
using Application.Users.Abstraction;

namespace Application.Telegram.Abstractions;

public interface ITelegramUnitOfWork
{
    IClientRepository ClientsRepository { get; }

    IUsersRepository UsersRepository { get; }

    ITelegramUserLinkRepository UserLinksRepository { get; }

    ITelegramInboundUpdateRepository InboundUpdatesRepository { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default);
}
