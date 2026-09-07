using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.ContactVerification.Abstractions;
using Application.ContactVerification.Errors;
using Application.Owners.Abstractions;
using Application.Owners.Enums;
using Application.Owners.Errors;
using Application.Security;
using Application.Verification.Abstractions;
using Domain.Clients.Entities;
using Domain.Clients.ValueObjects;
using Domain.ContactVerification.Enums;
using Domain.Users.ValueObjects;
using MediatR;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Owners.UseCases;

public sealed class RegisterOwnerCommandHandler(
    IUnitOfWork unitOfWork,
    IConsumeContactVerificationProof consumeProof,
    IRegisterOwnerSettings settings,
    IOtpProtector otpProtector) : IRequestHandler<RegisterOwnerCommand, RegisterOwnerResult>
{
    public async Task<RegisterOwnerResult> Handle(
        RegisterOwnerCommand request,
        CancellationToken cancellationToken)
    {
        var email = UserEmail.Create(request.Email).Value;
        var phone = ClientPhoneNumber.Create(request.PhoneNumber).Value;

        var clientRole = await unitOfWork.RolesRepository.GetByNameAsync(
            WebPlatformAccess.ClientRoleName,
            cancellationToken)
            ?? throw new NotFoundException(
                OwnerRegistrationErrors.ClientRoleMissing.Description);

        var emailInUse = await unitOfWork.UsersRepository.ExistsByEmailAsync(
            email,
            cancellationToken);
        if (emailInUse)
        {
            throw new ConflictException(
                "Ya existe un usuario con ese correo electrónico.",
                OwnerRegistrationErrors.EmailAlreadyInUse.Code);
        }

        var identificationInUse = await unitOfWork.ClientsRepository.ExistsByIdentificationNumberAsync(
            request.IdentificationNumber,
            cancellationToken);
        if (identificationInUse)
        {
            throw new ConflictException(
                "Ya existe un cliente con ese número de identificación.",
                OwnerRegistrationErrors.IdentificationAlreadyInUse.Code);
        }

        var phoneInUse = await unitOfWork.ClientsRepository.ExistsByPhoneAsync(
            phone,
            cancellationToken);
        if (phoneInUse)
        {
            throw new ConflictException(
                "Ya existe un cliente con ese número de teléfono.",
                OwnerRegistrationErrors.PhoneAlreadyInUse.Code);
        }

        RegisterOwnerResult? result = null;
        await unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            if (settings.RequiresContactProof(request.Channel))
            {
                await ConsumeRegisterProofAsync(email, request, transactionToken);
            }

            var user = new UserEntity(
                request.FullName.Trim(),
                email,
                passwordHash: null,
                clientRole.Id);

            await unitOfWork.UsersRepository.AddAsync(user, transactionToken);

            var client = new ClientEntity(
                user.Id,
                request.IdentificationNumber,
                request.Address,
                phoneNumber: phone);

            await unitOfWork.ClientsRepository.AddAsync(client, transactionToken);
            result = new RegisterOwnerResult(user.Id, client.Id);
        }, cancellationToken);

        return result!;
    }

    private async Task ConsumeRegisterProofAsync(
        string email,
        RegisterOwnerCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ContactProofSessionId is null
            || request.ContactProofSessionId == Guid.Empty
            || string.IsNullOrWhiteSpace(request.ContactProof))
        {
            throw new ContactVerificationException(OwnerRegistrationErrors.ProofRequired);
        }

        var consumed = await consumeProof.ConsumeAsync(
            new ConsumeContactVerificationProof(
                request.ContactProofSessionId.Value,
                request.ContactProof),
            cancellationToken);

        if (consumed.Purpose != ContactVerificationPurpose.Register)
        {
            throw new ContactVerificationException(OwnerRegistrationErrors.ProofPurposeInvalid);
        }

        var expectedHash = otpProtector.HashEmail(email);
        if (!string.Equals(consumed.DestinationHash, expectedHash, StringComparison.Ordinal))
        {
            throw new ContactVerificationException(OwnerRegistrationErrors.ProofEmailMismatch);
        }
    }
}
