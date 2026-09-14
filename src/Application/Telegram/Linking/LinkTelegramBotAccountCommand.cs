using Application.Telegram.Abstractions;
using Application.Telegram.Errors;
using Domain.Telegram.Entities;
using MediatR;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;

namespace Application.Telegram.Linking;

// Ticket 2.5: vincula el TelegramUserId real (leído del claim del token de
// invitado, nunca del body) con la persona ya registrada o encontrada por el
// bot, y crea la cuenta "fantasma" (sin password) que AgentDelegatedIdentityProvider.GetAsync
// exige para emitir un token delegado. No toca ProcessTelegramUpdateHandler:
// en cuanto el link exista, el mensaje siguiente fluye solo por la rama
// "userLink is not null" que ya existía antes de este ticket.
public sealed record LinkTelegramBotAccountCommand(
    Guid PersonId,
    long TelegramUserId) : IRequest<Guid>;

public sealed class LinkTelegramBotAccountHandler(
    ITelegramUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<LinkTelegramBotAccountCommand, Guid>
{
    public async Task<Guid> Handle(
        LinkTelegramBotAccountCommand request,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var user = await unitOfWork.UsersRepository.GetByIdAsync(
            request.PersonId,
            cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new TelegramAccountUnavailableException();
        }

        var existingLink = await unitOfWork.UserLinksRepository
            .GetByTelegramUserIdAsync(request.TelegramUserId, cancellationToken);
        if (existingLink is not null && existingLink.PersonId != request.PersonId)
        {
            throw new TelegramIdentityConflictException();
        }

        await EnsureGhostAccountAsync(user.Id, user.Email.Value, cancellationToken);

        var link = existingLink ?? await unitOfWork.UserLinksRepository
            .GetByPersonIdAsync(request.PersonId, cancellationToken);
        if (link is null)
        {
            link = TelegramUserLink.Create(
                request.PersonId,
                request.TelegramUserId,
                request.TelegramUserId,
                now);
            await unitOfWork.UserLinksRepository.AddAsync(link, cancellationToken);
        }
        else if (link.TelegramUserId != request.TelegramUserId ||
                  link.TelegramChatId != request.TelegramUserId)
        {
            link.Relink(request.TelegramUserId, request.TelegramUserId, now);
            await unitOfWork.UserLinksRepository.UpdateAsync(link, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return link.Id;
    }

    // Cuenta fantasma: mismo patrón que usaba el TelegramClientIdentityGateway
    // eliminado en el Ticket 1 (username generado, sin password, activa de una
    // vez) — solo que ahora se dispara desde este endpoint, no desde un OTP.
    private async Task EnsureGhostAccountAsync(
        Guid userId,
        string email,
        CancellationToken cancellationToken)
    {
        var account = await unitOfWork.UserAccountsRepository.GetByUserIdAsync(
            userId,
            cancellationToken);
        if (account is not null)
        {
            return;
        }

        string username;
        do
        {
            username = $"tg_{Guid.NewGuid():N}"[..30];
        }
        while (await unitOfWork.UserAccountsRepository.ExistsByUsernameAsync(
            username,
            cancellationToken));

        var ghostAccount = new UserAccountEntity(userId, username, email, "Activo");
        await unitOfWork.UserAccountsRepository.AddAsync(ghostAccount, cancellationToken);
    }
}
