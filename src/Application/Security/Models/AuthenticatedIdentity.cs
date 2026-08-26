namespace Application.Security.Models;
public sealed record AuthenticatedIdentity(
    Guid UserAccountId,
    Guid PersonId,
    Guid RoleId,
    string Role,
    string FullName,
    string UserName,
    string Email,
    string AccountStatus);