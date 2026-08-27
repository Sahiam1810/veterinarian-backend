using Application.ChatUserProfiles.Abstraction;
using Application.Common.Exceptions;
using MediatR;

namespace Application.ChatUserProfiles.UseCase;

public sealed record DeleteChatUserProfileCommand(Guid Id) : IRequest;

public sealed class DeleteChatUserProfileCommandHandler
    : IRequestHandler<DeleteChatUserProfileCommand>
{
    private readonly IChatUserProfileRepository _repository;

    public DeleteChatUserProfileCommandHandler(IChatUserProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(
        DeleteChatUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el perfil de chat '{request.Id}'.");

        // TODO: cuando existan participantes de conversación, validar que el perfil no esté referenciado.
        await _repository.DeleteAsync(profile, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
