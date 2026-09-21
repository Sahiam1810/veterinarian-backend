using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.ContactVerification.Abstractions;
using Application.ContactVerification.Errors;
using Application.Owners.Abstractions;
using Application.Owners.Enums;
using Application.Owners.Errors;
using Application.Verification.Abstractions;
using Domain.Clients.Entities;
using Domain.Clients.ValueObjects;
using Domain.ContactVerification.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Owners.UseCases;

public sealed class RegisterOwnerCommandHandler(
    IUnitOfWork unitOfWork,
    IConsumeContactVerificationProof consumeProof,
    IRegisterOwnerSettings settings,
    IOtpProtector otpProtector,
    ILogger<RegisterOwnerCommandHandler> logger) : IRequestHandler<RegisterOwnerCommand, RegisterOwnerResult>
{
    public async Task<RegisterOwnerResult> Handle(
        RegisterOwnerCommand request,
        CancellationToken cancellationToken)
    {
        var email = ClientEmail.Create(request.Email).Value;
        var phone = ClientPhoneNumber.Create(request.PhoneNumber).Value;

        var emailInUse = await unitOfWork.ClientsRepository.ExistsByEmailAsync(
            email,
            cancellationToken);
        if (emailInUse)
        {
            throw new ConflictException(
                "Ya existe un cliente con ese correo electrónico.",
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

            var client = new ClientEntity(
                request.FullName.Trim(),
                email,
                request.IdentificationNumber,
                phoneNumber: phone,
                address: request.Address);

            await unitOfWork.ClientsRepository.AddAsync(client, transactionToken);
            result = new RegisterOwnerResult(client.Id);
        }, cancellationToken);

        // Ids + canal; nunca email, teléfono, cédula ni proof.
        logger.LogInformation(
            "Owner registered. ClientId={ClientId} Channel={Channel}",
            result!.ClientId,
            request.Channel);

        return result;
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
