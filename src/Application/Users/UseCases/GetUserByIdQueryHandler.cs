using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Users.UseCase;

public sealed class GetUserByIdQueryHandler
    : IRequestHandler<GetUserByIdQuery, UserEntity>
{
    private readonly IUnitOfWork _uow;

    public GetUserByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<UserEntity> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _uow.UsersRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Usuario no encontrado.");
    }
}
