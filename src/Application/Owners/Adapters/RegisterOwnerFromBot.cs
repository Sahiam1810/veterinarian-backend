using Application.Owners.Abstractions;
using Application.Owners.Enums;
using Application.Owners.UseCases;
using MediatR;

namespace Application.Owners.Adapters;

// Adaptador bot (4.2). Siempre exige proof Email en el núcleo.
public sealed class RegisterOwnerFromBot(ISender sender) : IRegisterOwnerFromBot
{
    public Task<RegisterOwnerResult> RegisterAsync(
        RegisterOwnerFromBotRequest request,
        CancellationToken cancellationToken) =>
        sender.Send(
            new RegisterOwnerCommand(
                request.FullName,
                request.Email,
                request.IdentificationNumber,
                request.PhoneNumber,
                RegisterOwnerChannel.Bot,
                request.Address,
                request.ContactProofSessionId,
                request.ContactProof),
            cancellationToken);
}
