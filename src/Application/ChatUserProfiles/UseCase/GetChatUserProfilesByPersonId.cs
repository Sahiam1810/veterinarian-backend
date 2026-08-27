using Application.ChatUserProfiles.Abstraction;
using MediatR;
using ChatUserProfileEntity = Domain.ChatUserProfiles.Entities.ChatUserProfile;

namespace Application.ChatUserProfiles.UseCase;

public sealed record GetChatUserProfilesByPersonIdQuery(Guid PersonId)
    : IRequest<IReadOnlyCollection<ChatUserProfileEntity>>;

public sealed class GetChatUserProfilesByPersonIdQueryHandler
    : IRequestHandler<GetChatUserProfilesByPersonIdQuery, IReadOnlyCollection<ChatUserProfileEntity>>
{
    private readonly IChatUserProfileRepository _repository;

    public GetChatUserProfilesByPersonIdQueryHandler(IChatUserProfileRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyCollection<ChatUserProfileEntity>> Handle(
        GetChatUserProfilesByPersonIdQuery request,
        CancellationToken cancellationToken)
        => _repository.GetByPersonIdAsync(request.PersonId, cancellationToken);
}
