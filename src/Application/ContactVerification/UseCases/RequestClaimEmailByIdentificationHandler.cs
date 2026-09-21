using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.ContactVerification.Abstractions;
using Domain.ContactVerification.Enums;

namespace Application.ContactVerification.UseCases;

// Claim OTP por cédula: resuelve el email en servidor (lookup público no expone PII).
public sealed class RequestClaimEmailByIdentificationHandler(
    IUnitOfWork unitOfWork,
    IRequestContactEmailVerification requestEmail) : IRequestClaimEmailByIdentification
{
    public async Task<RequestClaimEmailByIdentificationResult> RequestAsync(
        RequestClaimEmailByIdentification request,
        CancellationToken cancellationToken)
    {
        var identification = (request.IdentificationNumber ?? string.Empty).Trim();
        if (identification.Length is < 5 or > 15 || !identification.All(char.IsDigit))
        {
            throw new NotFoundException("Cliente no encontrado.");
        }

        var client = await unitOfWork.ClientsRepository.GetByIdentificationNumberAsync(
            identification,
            cancellationToken);
        if (client is null)
        {
            throw new NotFoundException("Cliente no encontrado.");
        }

        var email = client.Email.Value;
        var otp = await requestEmail.RequestAsync(
            new RequestContactEmailVerification(
                email,
                ContactVerificationPurpose.Claim,
                client.UserId),
            cancellationToken);

        return new RequestClaimEmailByIdentificationResult(
            otp.SessionId,
            otp.ExpiresAt,
            otp.Channel,
            MaskEmail(email),
            client.UserId,
            client.Id,
            client.FullName.Value,
            email);
    }

    public static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 0 || at == email.Length - 1)
        {
            return "tu correo";
        }

        return $"{email[0]}***{email[at..]}";
    }
}
