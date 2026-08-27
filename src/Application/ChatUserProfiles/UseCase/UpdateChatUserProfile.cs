using Application.ChatUserProfiles.Abstraction;
using Application.Common.Exceptions;
using MediatR;
using ChatUserProfileEntity = Domain.ChatUserProfiles.Entities.ChatUserProfile;

namespace Application.ChatUserProfiles.UseCase;

public sealed record UpdateChatUserProfileCommand(
    Guid Id,
    string? DisplayName,
    string? AvatarUrl,
    string? Bio) : IRequest<ChatUserProfileEntity>;

public sealed class UpdateChatUserProfileCommandHandler
    : IRequestHandler<UpdateChatUserProfileCommand, ChatUserProfileEntity>
{
    private readonly IChatUserProfileRepository _repository;

    public UpdateChatUserProfileCommandHandler(IChatUserProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<ChatUserProfileEntity> Handle(
        UpdateChatUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el perfil de chat '{request.Id}'.");

        profile.Update(request.DisplayName, request.AvatarUrl, request.Bio);

        await _repository.UpdateAsync(profile, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return profile;
    }
}
