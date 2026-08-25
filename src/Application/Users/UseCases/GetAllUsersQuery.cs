using MediatR;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Users.UseCase;

public sealed record GetAllUsersQuery
    : IRequest<IReadOnlyCollection<UserEntity>>;
