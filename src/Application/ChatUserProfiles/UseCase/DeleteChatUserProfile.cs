using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.ChatUserProfiles.UseCase;

public sealed record DeleteChatUserProfileCommand(Guid Id) : IRequest;

public sealed class DeleteChatUserProfileCommandHandler
    : IRequestHandler<DeleteChatUserProfileCommand>
{
    private readonly IUnitOfWork _uow;

    public DeleteChatUserProfileCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(
        DeleteChatUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await _uow.ChatUserProfilesRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el perfil de chat '{request.Id}'.");

        // TODO: cuando existan participantes de conversación, validar que el perfil no esté referenciado.
        await _uow.ChatUserProfilesRepository.DeleteAsync(profile, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
