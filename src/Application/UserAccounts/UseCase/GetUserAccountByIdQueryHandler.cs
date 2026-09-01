using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;

namespace Application.UserAccounts.UseCase;

public sealed class GetUserAccountByIdQueryHandler
    : IRequestHandler<GetUserAccountByIdQuery, UserAccountEntity>
{
    private readonly IUnitOfWork _uow;

    public GetUserAccountByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<UserAccountEntity> Handle(
        GetUserAccountByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _uow.UserAccountsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Cuenta de usuario no encontrada.");
    }
}
