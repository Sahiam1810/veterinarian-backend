using Application.Common.Abstractions;
using MediatR;
using ChatUserProfileEntity = Domain.ChatUserProfiles.Entities.ChatUserProfile;

namespace Application.ChatUserProfiles.UseCase;

public sealed record GetChatUserProfileByIdQuery(Guid Id) : IRequest<ChatUserProfileEntity?>;

public sealed class GetChatUserProfileByIdQueryHandler
    : IRequestHandler<GetChatUserProfileByIdQuery, ChatUserProfileEntity?>
{
    private readonly IUnitOfWork _uow;

    public GetChatUserProfileByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<ChatUserProfileEntity?> Handle(
        GetChatUserProfileByIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatUserProfilesRepository.GetByIdAsync(request.Id, cancellationToken);
}
