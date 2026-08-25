using MediatR;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Users.UseCase;

public sealed record GetUserByIdQuery(Guid Id)
    : IRequest<UserEntity?>;
