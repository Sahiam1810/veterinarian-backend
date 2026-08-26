using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using UserCredentialsEntity = Domain.UserCredentials.Entities.UserCredentials;

namespace Application.UserCredentials.UseCase;

public sealed class CreateUserCredentialsCommandHandler
    : IRequestHandler<CreateUserCredentialsCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserCredentialsCommandHandler(
        IUnitOfWork uow,
        IPasswordHasher passwordHasher)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(
        CreateUserCredentialsCommand request,
        CancellationToken cancellationToken)
    {
        var account = await _uow.UserAccountsRepository.GetByIdAsync(
            request.AccountId,
            cancellationToken);

        if (account is null)
        {
            throw new NotFoundException(
                "La cuenta especificada no existe.");
        }

        var credentialsExist = await _uow.UserCredentialsRepository.ExistsByAccountIdAsync(
            request.AccountId,
            cancellationToken);

        if (credentialsExist)
        {
            throw new ConflictException(
                "La cuenta ya tiene credenciales configuradas.");
        }

        var passwordHash = _passwordHasher.Hash(request.Password);

        var credentials = new UserCredentialsEntity(
            request.AccountId,
            passwordHash);

        await _uow.UserCredentialsRepository.AddAsync(
            credentials,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return credentials.Id;
    }
}
