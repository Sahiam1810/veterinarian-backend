using Application.Common.Abstractions;
using MediatR;
using ChatUserProfileEntity = Domain.ChatUserProfiles.Entities.ChatUserProfile;

namespace Application.ChatUserProfiles.UseCase;

public sealed record GetChatUserProfilesByPersonIdQuery(Guid PersonId)
    : IRequest<IReadOnlyCollection<ChatUserProfileEntity>>;

public sealed class GetChatUserProfilesByPersonIdQueryHandler
    : IRequestHandler<GetChatUserProfilesByPersonIdQuery, IReadOnlyCollection<ChatUserProfileEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetChatUserProfilesByPersonIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<ChatUserProfileEntity>> Handle(
        GetChatUserProfilesByPersonIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatUserProfilesRepository.GetByPersonIdAsync(request.PersonId, cancellationToken);
}
