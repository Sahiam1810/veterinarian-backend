using Application.Common.Abstractions;
using MediatR;
using ChatUserProfileEntity = Domain.ChatUserProfiles.Entities.ChatUserProfile;

namespace Application.ChatUserProfiles.UseCase;

public sealed record GetChatUserProfilesByUserIdQuery(Guid UserId)
    : IRequest<IReadOnlyCollection<ChatUserProfileEntity>>;

public sealed class GetChatUserProfilesByUserIdQueryHandler
    : IRequestHandler<GetChatUserProfilesByUserIdQuery, IReadOnlyCollection<ChatUserProfileEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetChatUserProfilesByUserIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<ChatUserProfileEntity>> Handle(
        GetChatUserProfilesByUserIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatUserProfilesRepository.GetByUserIdAsync(request.UserId, cancellationToken);
}
