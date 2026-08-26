using Api.UserCredentials.Dtos;
using Application.UserCredentials.UseCase;
using UserCredentialsEntity = Domain.UserCredentials.Entities.UserCredentials;

namespace Api.UserCredentials.Mappings;

public static class UserCredentialsMappings
{
    public static CreateUserCredentialsCommand ToCommand(
        this CreateUserCredentialsRequest request)
    {
        return new CreateUserCredentialsCommand(
            request.AccountId,
            request.Password);
    }

    public static ChangePasswordCommand ToCommand(
        this ChangePasswordRequest request,
        Guid id)
    {
        return new ChangePasswordCommand(
            id,
            request.CurrentPassword,
            request.NewPassword);
    }

    public static UserCredentialsResponse ToResponse(this UserCredentialsEntity credentials)
    {
        return new UserCredentialsResponse(
            credentials.Id,
            credentials.AccountId,
            credentials.LastChanged,
            credentials.CreatedAt);
    }
}
