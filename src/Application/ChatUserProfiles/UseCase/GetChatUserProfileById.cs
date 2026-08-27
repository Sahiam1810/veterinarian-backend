using Application.ChatUserProfiles.Abstraction;
using MediatR;
using ChatUserProfileEntity = Domain.ChatUserProfiles.Entities.ChatUserProfile;

namespace Application.ChatUserProfiles.UseCase;

public sealed record GetChatUserProfileByIdQuery(Guid Id) : IRequest<ChatUserProfileEntity?>;

public sealed class GetChatUserProfileByIdQueryHandler
    : IRequestHandler<GetChatUserProfileByIdQuery, ChatUserProfileEntity?>
{
    private readonly IChatUserProfileRepository _repository;

    public GetChatUserProfileByIdQueryHandler(IChatUserProfileRepository repository)
    {
        _repository = repository;
    }

    public Task<ChatUserProfileEntity?> Handle(
        GetChatUserProfileByIdQuery request,
        CancellationToken cancellationToken)
        => _repository.GetByIdAsync(request.Id, cancellationToken);
}
