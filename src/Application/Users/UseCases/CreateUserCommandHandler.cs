using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Users.UseCase;

public sealed class CreateUserCommandHandler
    : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserCommandHandler(
        IUnitOfWork uow,
        IPasswordHasher passwordHasher)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var role = await _uow.RolesRepository.GetByIdAsync(
            request.RoleId,
            cancellationToken);

        if (role is null)
        {
            throw new NotFoundException(
                "El rol especificado no existe.");
        }

        var emailInUse = await _uow.UsersRepository.ExistsByEmailAsync(
            request.Email,
            cancellationToken);

        if (emailInUse)
        {
            throw new ConflictException(
                "Ya existe un usuario con ese correo electrónico.");
        }

        // Cliente nunca se loguea (solo interactúa vía chatbot): sin contraseña.
        var isClientRole = string.Equals(role.Name.Value, "Cliente", StringComparison.Ordinal);
        var passwordHash = isClientRole ? null : _passwordHasher.Hash(request.Password!);

        var user = new UserEntity(
            request.FullName,
            request.Email,
            passwordHash,
            request.RoleId);

        await _uow.UsersRepository.AddAsync(
            user,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return user.Id;
    }
}
