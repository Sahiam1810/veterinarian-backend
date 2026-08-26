using Api.UserAccounts.Dtos;
using Application.UserAccounts.UseCase;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;

namespace Api.UserAccounts.Mappings;

public static class UserAccountsMappings
{
    public static CreateUserAccountCommand ToCommand(
        this CreateUserAccountRequest request)
    {
        return new CreateUserAccountCommand(
            request.UserId,
            request.Username,
            request.Mail,
            request.Status);
    }

    public static UpdateUserAccountCommand ToCommand(
        this UpdateUserAccountRequest request,
        Guid id)
    {
        return new UpdateUserAccountCommand(
            id,
            request.Username,
            request.Mail,
            request.Status);
    }

    public static UserAccountResponse ToResponse(this UserAccountEntity account)
    {
        return new UserAccountResponse(
            account.Id,
            account.UserId,
            account.Username.Value,
            account.Mail.Value,
            account.Status,
            account.CreatedAt);
    }

    public static IReadOnlyCollection<UserAccountResponse> ToResponse(
        this IReadOnlyCollection<UserAccountEntity> accounts)
    {
        return accounts
            .Select(account => account.ToResponse())
            .ToArray();
    }
}
