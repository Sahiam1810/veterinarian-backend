using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Roles;
using MediatR;

namespace Application.Users.UseCase;

// Borra User + perfil vet/cliente + notificaciones/chat (sin vistas separadas obligatorias).
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

            // Cliente: solo si no tiene mascotas (activas o inactivas) a su nombre
            var clientId = await uow.ClientsRepository.GetIdByUserIdAsync(user.Id, ct);
            if (clientId.HasValue)
            {
                var links = await uow.ClientPetsRepository.GetByClientIdAsync(clientId.Value, ct);
                if (links.Count > 0)
                {
                    throw new ConflictException(
                        "El cliente tiene mascotas asociadas. Elimínalas desde Mascotas antes de borrar al cliente.");
                }

                await uow.ClientsRepository.DeleteByUserIdAsync(user.Id, ct);
            }

            await uow.NotificationsRepository.DeleteByUserIdAsync(user.Id, ct);

            var chatProfiles = await uow.ChatUserProfilesRepository.GetByUserIdAsync(
                user.Id,
                ct);
            foreach (var profile in chatProfiles)
            {
                await uow.ChatUserProfilesRepository.DeleteAsync(profile, ct);
            }

            await uow.UsersRepository.DeleteAsync(user, ct);
        }, cancellationToken);
    }
}
