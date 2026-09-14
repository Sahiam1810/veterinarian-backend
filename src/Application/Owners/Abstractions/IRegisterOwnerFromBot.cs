using Application.Owners.Abstractions;

namespace Application.Owners.Abstractions;

public sealed record RegisterOwnerFromBotRequest(
    string FullName,
    string Email,
    string IdentificationNumber,
    string PhoneNumber,
    string? Address = null);

public interface IRegisterOwnerFromBot
{
    Task<RegisterOwnerResult> RegisterAsync(
        RegisterOwnerFromBotRequest request,
        CancellationToken cancellationToken);
}
