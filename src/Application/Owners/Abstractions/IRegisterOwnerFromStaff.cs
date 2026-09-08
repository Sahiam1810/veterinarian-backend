namespace Application.Owners.Abstractions;

public sealed record RegisterOwnerFromStaffRequest(
    string FullName,
    string Email,
    string IdentificationNumber,
    string PhoneNumber,
    string? Address = null,
    Guid? ContactProofSessionId = null,
    string? ContactProof = null);

public interface IRegisterOwnerFromStaff
{
    Task<RegisterOwnerResult> RegisterAsync(
        RegisterOwnerFromStaffRequest request,
        CancellationToken cancellationToken);
}
