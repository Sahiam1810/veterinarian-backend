using MediatR;

namespace Application.Telegram.Linking;

// Vincula el TelegramUserId real con el cliente registrado o encontrado por el bot.
public sealed record LinkTelegramBotAccountCommand(
    Guid ClientId,
    long TelegramUserId) : IRequest<Guid>;

public sealed class LinkTelegramBotAccountHandler(
    TelegramBotAccountLinker linker,
    Application.Telegram.Abstractions.ITelegramRuntimeSettings settings)
    : IRequestHandler<LinkTelegramBotAccountCommand, Guid>
{
    public async Task<Guid> Handle(
        LinkTelegramBotAccountCommand request,
        CancellationToken cancellationToken)
    {
        var result = await linker.LinkAsync(
            request.ClientId,
            request.TelegramUserId,
            requireRecentRegistration: true,
            settings.RegistrationLinkWindow,
            cancellationToken);
        return result.LinkId;
    }
}

public sealed record LinkTelegramBotAccountWithProofCommand(
    Guid SessionId,
    string Proof,
    long TelegramUserId) : IRequest<TelegramBotAccountLinkResult>;

public sealed class LinkTelegramBotAccountWithProofHandler(
    Application.ContactVerification.Abstractions.IConsumeContactVerificationProof proofConsumer,
    TelegramBotAccountLinker linker)
    : IRequestHandler<LinkTelegramBotAccountWithProofCommand, TelegramBotAccountLinkResult>
{
    public async Task<TelegramBotAccountLinkResult> Handle(
        LinkTelegramBotAccountWithProofCommand request,
        CancellationToken cancellationToken)
    {
        var consumed = await proofConsumer.ConsumeAsync(
            new Application.ContactVerification.Abstractions.ConsumeContactVerificationProof(
                request.SessionId,
                request.Proof),
            cancellationToken);

        if (consumed.Purpose != Domain.ContactVerification.Enums.ContactVerificationPurpose.Claim ||
            consumed.SubjectUserId is null ||
            consumed.SubjectUserId == Guid.Empty)
        {
            throw new Application.ContactVerification.Errors.ContactVerificationException(
                Application.ContactVerification.Errors.ContactVerificationErrors.PurposeInvalid);
        }

        return await linker.LinkAsync(
            consumed.SubjectUserId.Value,
            request.TelegramUserId,
            requireRecentRegistration: false,
            TimeSpan.Zero,
            cancellationToken);
    }
}
