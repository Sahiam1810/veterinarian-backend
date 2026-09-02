using Application.Common.Results;
using Application.Security.Registration;
using Application.Telegram.Abstractions;
using Domain.Telegram.Entities;
using MediatR;

namespace Application.Telegram.Registration;

public sealed record CompleteTelegramRegistrationCommand(
    string Token,
    string FullName,
    string IdentificationNumber,
    string UserName,
    string Password,
    string PasswordConfirmation)
    : IRequest<Result<CompletedTelegramRegistration>>;

public sealed record CompletedTelegramRegistration(Guid PersonId, long TelegramChatId);

public sealed class CompleteTelegramRegistrationCommandHandler(
    ITelegramUnitOfWork unitOfWork,
    ITelegramRegistrationProtector protector,
    IClientAccountRegistrationService registration,
    TimeProvider timeProvider)
    : IRequestHandler<CompleteTelegramRegistrationCommand, Result<CompletedTelegramRegistration>>
{
    public async Task<Result<CompletedTelegramRegistration>> Handle(
        CompleteTelegramRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        var tokenHash = protector.HashCompletionToken(request.Token);
        var session = await unitOfWork.RegistrationSessionsRepository
            .GetByCompletionTokenHashAsync(tokenHash, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (session?.CompletionExpiresAt is null ||
            now >= session.CompletionExpiresAt ||
            string.IsNullOrWhiteSpace(session.ProtectedEmail))
        {
            return Result<CompletedTelegramRegistration>.Failure(
                TelegramRegistrationErrors.InvalidOrExpired);
        }

        Result<CompletedTelegramRegistration>? completed = null;
        Error? failure = null;
        await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            var activeLink = await unitOfWork.UserLinksRepository.GetByTelegramUserIdAsync(
                session.TelegramUserId, transactionToken);
            if (activeLink is not null)
            {
                failure = TelegramRegistrationErrors.IdentityConflict;
                return;
            }

            var account = await registration.StageAsync(
                new ClientAccountRegistrationRequest(
                    request.FullName,
                    protector.UnprotectEmail(session.ProtectedEmail),
                    request.UserName,
                    request.Password,
                    request.IdentificationNumber),
                transactionToken);
            if (account.IsFailure)
            {
                failure = account.Error;
                return;
            }

            var link = TelegramUserLink.Create(
                account.Value.PersonId,
                session.TelegramUserId,
                session.TelegramChatId,
                now);
            await unitOfWork.UserLinksRepository.AddAsync(link, transactionToken);
            session.Complete(account.Value.PersonId, now);
            await unitOfWork.RegistrationSessionsRepository.UpdateAsync(session, transactionToken);
            completed = Result<CompletedTelegramRegistration>.Success(
                new CompletedTelegramRegistration(account.Value.PersonId, session.TelegramChatId));
        }, cancellationToken);

        return failure is not null
            ? Result<CompletedTelegramRegistration>.Failure(failure)
            : completed!;
    }
}
