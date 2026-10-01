using Application.Common.Abstractions;
using Domain.Roles;
using MediatR;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Users.UseCase;

public sealed class GetAllUsersQueryHandler
    : IRequestHandler<
        GetAllUsersQuery,
        IReadOnlyCollection<UserEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetAllUsersQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyCollection<UserEntity>> Handle(
        GetAllUsersQuery request,
        CancellationToken cancellationToken)
    {
        var users = await _uow.UsersRepository.GetAllAsync(
            cancellationToken);

        return users
            .Where(user => user.RoleId != SystemRoles.ClientRoleId)
            .ToArray();
    }
}
