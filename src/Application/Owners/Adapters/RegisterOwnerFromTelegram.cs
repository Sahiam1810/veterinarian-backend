using Application.Owners.Abstractions;
using Application.Owners.Enums;
using Application.Owners.UseCases;
using MediatR;

namespace Application.Owners.Adapters;

// Adaptador Telegram (4.3). TelegramUserId queda para el enlace posterior; el núcleo no crea login.
public sealed class RegisterOwnerFromTelegram(ISender sender) : IRegisterOwnerFromTelegram
{
    public Task<RegisterOwnerResult> RegisterAsync(
        RegisterOwnerFromTelegramRequest request,
        CancellationToken cancellationToken) =>
        sender.Send(
            new RegisterOwnerCommand(
                request.FullName,
                request.Email,
                request.IdentificationNumber,
                request.PhoneNumber,
                RegisterOwnerChannel.Telegram,
                request.Address,
                request.ContactProofSessionId,
                request.ContactProof),
            cancellationToken);
}
