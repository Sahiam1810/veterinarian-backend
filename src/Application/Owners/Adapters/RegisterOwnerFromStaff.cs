using Application.Owners.Abstractions;
using Application.Owners.Enums;
using Application.Owners.UseCases;
using MediatR;

namespace Application.Owners.Adapters;

// Adaptador staff (4.1 HTTP). El flag RequireContactProofs lo interpreta el núcleo.
public sealed class RegisterOwnerFromStaff(ISender sender) : IRegisterOwnerFromStaff
{
    public Task<RegisterOwnerResult> RegisterAsync(
        RegisterOwnerFromStaffRequest request,
        CancellationToken cancellationToken) =>
        sender.Send(
            new RegisterOwnerCommand(
                request.FullName,
                request.Email,
                request.IdentificationNumber,
                request.PhoneNumber,
                RegisterOwnerChannel.Staff,
                request.Address,
                request.ContactProofSessionId,
                request.ContactProof),
            cancellationToken);
}
