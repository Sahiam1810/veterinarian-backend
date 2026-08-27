using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatUserProfileEntity = Domain.ChatUserProfiles.Entities.ChatUserProfile;

namespace Application.ChatUserProfiles.UseCase;

public sealed record CreateChatUserProfileCommand(
    Guid PersonId,
    string? DisplayName,
    string? AvatarUrl,
    string? Bio) : IRequest<ChatUserProfileEntity>;

public sealed class CreateChatUserProfileCommandHandler
    : IRequestHandler<CreateChatUserProfileCommand, ChatUserProfileEntity>
{
    private readonly IUnitOfWork _uow;

    public CreateChatUserProfileCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatUserProfileEntity> Handle(
        CreateChatUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _uow.UsersRepository.GetByIdAsync(request.PersonId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException(
                $"No se encontró el usuario '{request.PersonId}'.");
        }

        var profile = ChatUserProfileEntity.Create(
            request.PersonId,
            request.DisplayName,
            request.AvatarUrl,
            request.Bio);

        await _uow.ChatUserProfilesRepository.AddAsync(profile, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return profile;
    }
}
