using Application.Common.Abstractions;
using MediatR;
using ChatEscalationResolutionEntity = Domain.ChatEscalationResolutions.Entities.ChatEscalationResolution;

namespace Application.ChatEscalationResolutions.UseCase;

public sealed record GetAllChatEscalationResolutionsQuery
    : IRequest<IReadOnlyCollection<ChatEscalationResolutionEntity>>;

public sealed class GetAllChatEscalationResolutionsQueryHandler
    : IRequestHandler<GetAllChatEscalationResolutionsQuery, IReadOnlyCollection<ChatEscalationResolutionEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetAllChatEscalationResolutionsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<ChatEscalationResolutionEntity>> Handle(
        GetAllChatEscalationResolutionsQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatEscalationResolutionsRepository.GetAllAsync(cancellationToken);
}
