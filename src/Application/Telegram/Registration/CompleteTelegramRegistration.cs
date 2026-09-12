using Application.Common.Exceptions;
using Application.Common.Results;
using Application.Owners.Abstractions;
using Application.Telegram.Abstractions;
using Domain.Telegram.Entities;
using MediatR;

namespace Application.Telegram.Registration;

// Completado Telegram sin password: RegisterOwner (User sin hash + Client) + enlace chat.
// Equivalencia ADR: OTP Gmail de la sesión sustituye ContactVerification ConsumeProof.
public sealed record CompleteTelegramRegistrationCommand(
    string Token,
    string FullName,
    string IdentificationNumber,
    string PhoneNumber,
    string? Address = null)
    : IRequest<Result<CompletedTelegramRegistration>>;

public sealed record CompletedTelegramRegistration(Guid PersonId, long TelegramChatId);

public sealed class CompleteTelegramRegistrationCommandHandler(
    ITelegramUnitOfWork unitOfWork,
    ITelegramRegistrationProtector protector,
    IRegisterOwnerFromTelegram registerOwner,
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

            try
            {
                // Canal Telegram: sin proof Etapa 3; la sesión ya verificó el correo.
                var registerResult = await registerOwner.RegisterAsync(
                    new RegisterOwnerFromTelegramRequest(
                        request.FullName,
                        email,
                        request.IdentificationNumber,
                        request.PhoneNumber,
                        session.TelegramUserId,
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
            catch (ConflictException ex)
            {
                // Conserva code estable del núcleo (email/cédula/teléfono).
                failure = new Error(
                    string.IsNullOrWhiteSpace(ex.Code) ? "Registration.Failed" : ex.Code,
                    ex.Message);
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
