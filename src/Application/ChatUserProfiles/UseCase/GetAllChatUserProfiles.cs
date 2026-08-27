using Application.Common.Abstractions;
using MediatR;
using ChatUserProfileEntity = Domain.ChatUserProfiles.Entities.ChatUserProfile;

namespace Application.ChatUserProfiles.UseCase;

public sealed record GetAllChatUserProfilesQuery
    : IRequest<IReadOnlyCollection<ChatUserProfileEntity>>;

public sealed class GetAllChatUserProfilesQueryHandler
    : IRequestHandler<GetAllChatUserProfilesQuery, IReadOnlyCollection<ChatUserProfileEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetAllChatUserProfilesQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<ChatUserProfileEntity>> Handle(
        GetAllChatUserProfilesQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatUserProfilesRepository.GetAllAsync(cancellationToken);
}
