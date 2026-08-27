using Application.ChatUserProfiles.Abstraction;
using Application.Common.Exceptions;
using Application.Users.Abstraction;
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
    private readonly IChatUserProfileRepository _profileRepository;
    private readonly IUsersRepository _usersRepository;

    public CreateChatUserProfileCommandHandler(
        IChatUserProfileRepository profileRepository,
        IUsersRepository usersRepository)
    {
        _profileRepository = profileRepository;
        _usersRepository = usersRepository;
    }

    public async Task<ChatUserProfileEntity> Handle(
        CreateChatUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _usersRepository.GetByIdAsync(request.PersonId, cancellationToken);
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

        await _profileRepository.AddAsync(profile, cancellationToken);
        await _profileRepository.SaveChangesAsync(cancellationToken);

        return profile;
    }
}
