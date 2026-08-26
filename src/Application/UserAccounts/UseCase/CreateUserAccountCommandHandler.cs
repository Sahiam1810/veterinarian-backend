using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using UserAccountEntity = Domain.UserAccounts.Entities.UserAccounts;

namespace Application.UserAccounts.UseCase;

public sealed class CreateUserAccountCommandHandler
    : IRequestHandler<CreateUserAccountCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateUserAccountCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(
        CreateUserAccountCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _uow.UsersRepository.GetByIdAsync(
            request.UserId,
            cancellationToken);

        if (user is null)
        {
            throw new NotFoundException(
                "El usuario especificado no existe.");
        }

        var userAlreadyHasAccount = await _uow.UserAccountsRepository.ExistsByUserIdAsync(
            request.UserId,
            cancellationToken);

        if (userAlreadyHasAccount)
        {
            throw new ConflictException(
                "El usuario ya tiene una cuenta asociada.");
        }

        var usernameInUse = await _uow.UserAccountsRepository.ExistsByUsernameAsync(
            request.Username,
            cancellationToken);

        if (usernameInUse)
        {
            throw new ConflictException(
                "Ya existe una cuenta con ese nombre de usuario.");
        }

        var account = new UserAccountEntity(
            request.UserId,
            request.Username,
            request.Mail,
            request.Status);

        await _uow.UserAccountsRepository.AddAsync(
            account,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return account.Id;
    }
}
