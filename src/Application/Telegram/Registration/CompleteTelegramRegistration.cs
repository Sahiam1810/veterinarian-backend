using Application.Common.Results;
using Application.Owners.Enums;
using Application.Owners.UseCases;
using Application.Telegram.Abstractions;
using Domain.Telegram.Entities;
using MediatR;

namespace Application.Telegram.Registration;

// Completado de registro desde Telegram (sin usuario ni contraseña).
// Registra el dueño a través del núcleo RegisterOwner (User sin hash + Client sin credentials) y vincula la cuenta a Telegram.
public sealed record CompleteTelegramRegistrationCommand(
    string Token,
    string FullName,
    string IdentificationNumber,
    string? PhoneNumber = null,
    string? Address = null)
    : IRequest<Result<CompletedTelegramRegistration>>;

public sealed record CompletedTelegramRegistration(Guid PersonId, long TelegramChatId);

public sealed class CompleteTelegramRegistrationCommandHandler(
    ITelegramUnitOfWork unitOfWork,
    ITelegramRegistrationProtector protector,
    ISender sender,
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

            var email = protector.UnprotectEmail(session.ProtectedEmail);
            var phone = !string.IsNullOrWhiteSpace(request.PhoneNumber)
                ? request.PhoneNumber
                : "3000000000";

            try
            {
                var registerResult = await sender.Send(
                    new RegisterOwnerCommand(
                        request.FullName,
                        email,
                        request.IdentificationNumber,
                        phone,
                        RegisterOwnerChannel.Telegram,
                        request.Address),
                    transactionToken);

                var link = TelegramUserLink.Create(
                    registerResult.UserId,
                    session.TelegramUserId,
                    session.TelegramChatId,
                    now);

                await unitOfWork.UserLinksRepository.AddAsync(link, transactionToken);
                session.Complete(registerResult.UserId, now);
                await unitOfWork.RegistrationSessionsRepository.UpdateAsync(session, transactionToken);

                completed = Result<CompletedTelegramRegistration>.Success(
                    new CompletedTelegramRegistration(registerResult.UserId, session.TelegramChatId));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                failure = new Error("Registration.Failed", ex.Message);
            }
        }, cancellationToken);

        return failure is not null
            ? Result<CompletedTelegramRegistration>.Failure(failure)
            : completed!;
    }
}
