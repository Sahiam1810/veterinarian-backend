using Api.Users.Dtos;
using Application.Users.UseCase;
using UserEntity = Domain.Users.Entities.Users;

namespace Api.Users.Mappings;

public static class UsersMappings
{
    public static CreateUserCommand ToCommand(
        this CreateUserRequest request)
    {
        return new CreateUserCommand(
            request.FullName,
            request.Email,
            request.Password,
            request.RoleId);
    }

    public static UpdateUserCommand ToCommand(
        this UpdateUserRequest request,
        Guid id)
    {
        return new UpdateUserCommand(
            id,
            request.FullName,
            request.Email,
            request.RoleId);
    }

    public static UserResponse ToResponse(this UserEntity user)
    {
        return new UserResponse(
            user.Id,
            user.FullName,
            user.Email.Value,
            user.RoleId,
            user.IsActive,
            user.CreatedAt);
    }

    public static IReadOnlyCollection<UserResponse> ToResponse(
        this IReadOnlyCollection<UserEntity> users)
    {
        return users
            .Select(user => user.ToResponse())
            .ToArray();
    }
}
