using Application.ChatUserProfiles.Abstraction;
using MediatR;
using ChatUserProfileEntity = Domain.ChatUserProfiles.Entities.ChatUserProfile;

namespace Application.ChatUserProfiles.UseCase;

public sealed record GetAllChatUserProfilesQuery
    : IRequest<IReadOnlyCollection<ChatUserProfileEntity>>;

public sealed class GetAllChatUserProfilesQueryHandler
    : IRequestHandler<GetAllChatUserProfilesQuery, IReadOnlyCollection<ChatUserProfileEntity>>
{
    private readonly IChatUserProfileRepository _repository;

    public GetAllChatUserProfilesQueryHandler(IChatUserProfileRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyCollection<ChatUserProfileEntity>> Handle(
        GetAllChatUserProfilesQuery request,
        CancellationToken cancellationToken)
        => _repository.GetAllAsync(cancellationToken);
}
