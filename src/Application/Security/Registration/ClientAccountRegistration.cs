using Application.Common.Results;

namespace Application.Security.Registration;

public sealed record ClientAccountRegistrationRequest(
    string FullName,
    string Email,
    string UserName,
    string Password,
    string IdentificationNumber);

public sealed record RegisteredClientAccount(
    Guid PersonId,
    Guid UserAccountId,
    Guid RoleId,
    string RoleName,
    string FullName,
    string UserName,
    string Email,
    string Status);

public interface IClientAccountRegistrationService
{
    Task<Result<RegisteredClientAccount>> StageAsync(
        ClientAccountRegistrationRequest request,
        CancellationToken cancellationToken);
}
