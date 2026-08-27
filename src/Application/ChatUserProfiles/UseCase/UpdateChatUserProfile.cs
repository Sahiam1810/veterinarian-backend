using Application.Common.Abstractions;
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
    private readonly IUnitOfWork _uow;

    public UpdateChatUserProfileCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatUserProfileEntity> Handle(
        UpdateChatUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await _uow.ChatUserProfilesRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el perfil de chat '{request.Id}'.");

        profile.Update(request.DisplayName, request.AvatarUrl, request.Bio);

        await _uow.ChatUserProfilesRepository.UpdateAsync(profile, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return profile;
    }
}
