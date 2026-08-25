using Application.Common.Abstractions;
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
        return await _uow.UsersRepository.GetAllAsync(
            cancellationToken);
    }
}
