using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Clients.UseCases;

// Actualiza nombre/correo del User asociado a un Client sin exigir permiso
// sobre "Usuarios" ni que el caller resuelva/reenvíe el RoleId (que no
// cambia). Pensado para roles que solo tienen permiso sobre "Clientes" (ej.
// Recepcionista) y no pueden llamar PUT /api/Users directamente -- mismo
// principio que POST /api/Clients/register-owner (combinar Client+User bajo
// un único permiso de "Clientes").
public sealed record UpdateClientOwnerProfileCommand(
    Guid ClientId,
    string FullName,
    string Email) : IRequest;

public sealed class UpdateClientOwnerProfileCommandHandler
    : IRequestHandler<UpdateClientOwnerProfileCommand>
{
    private readonly IUnitOfWork _uow;

    public UpdateClientOwnerProfileCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(
        UpdateClientOwnerProfileCommand request,
        CancellationToken cancellationToken)
    {
        var client = await _uow.ClientsRepository.GetByIdAsync(request.ClientId, cancellationToken)
            ?? throw new NotFoundException("Cliente no encontrado.");

        var user = await _uow.UsersRepository.GetByIdAsync(client.UserId, cancellationToken)
            ?? throw new NotFoundException("Usuario no encontrado.");

        var emailInUse = await _uow.UsersRepository.ExistsByEmailAsync(
            request.Email,
            cancellationToken,
            user.Id);

        if (emailInUse)
        {
            throw new ConflictException("Ya existe un usuario con ese correo electrónico.");
        }

        // Mismo rol de siempre: este endpoint nunca cambia el rol del dueño.
        user.Update(request.FullName, request.Email, user.RoleId);

        await _uow.UsersRepository.UpdateAsync(user, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
