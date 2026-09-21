using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Roles;
using MediatR;

namespace Application.Users.UseCase;

// Borra User + perfil vet + notificaciones/chat (sin vistas separadas obligatorias).
public sealed class DeleteUserCommandHandler(IUnitOfWork uow)
    : IRequestHandler<DeleteUserCommand>
{
    public async Task Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken)
    {
        await uow.ExecuteInTransactionAsync(async ct =>
        {
            var user = await uow.UsersRepository.GetByIdAsync(
                request.Id,
                ct)
                ?? throw new NotFoundException("Usuario no encontrado.");

            if (SystemRoles.IsSuperAdmin(user.RoleId))
            {
                throw new ForbiddenException(
                    "La cuenta SuperAdmin no se puede eliminar.");
            }

            if (user.IsActive)
            {
                throw new ConflictException(
                    "Desactiva el usuario antes de eliminarlo.");
            }

            var agents = await uow.AgentHumansRepository.GetByUserIdAsync(
                user.Id,
                ct);
            if (agents.Count > 0)
            {
                throw new ConflictException(
                    "El usuario está vinculado como agente humano y no se puede eliminar aún.");
            }

            var veterinarianId = await uow.VeterinariansRepository.GetIdByUserIdAsync(
                user.Id,
                ct);
            if (veterinarianId.HasValue)
            {
                var appointments = await uow.AppointmentsRepository.GetByVeterinarianIdAsync(
                    veterinarianId.Value,
                    cancellationToken: ct);
                if (appointments.Count > 0)
                {
                    throw new ConflictException(
                        "El veterinario tiene citas asociadas. Cancélalas o reasígnelas antes de eliminarlo.");
                }

                await uow.VeterinariansRepository.DeleteByUserIdAsync(user.Id, ct);
            }

            await uow.NotificationsRepository.DeleteByUserIdAsync(user.Id, ct);

            await uow.UsersRepository.DeleteAsync(user, ct);
        }, cancellationToken);
    }
}
