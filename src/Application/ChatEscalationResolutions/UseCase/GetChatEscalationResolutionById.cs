using Application.Common.Abstractions;
using MediatR;
using ChatEscalationResolutionEntity = Domain.ChatEscalationResolutions.Entities.ChatEscalationResolution;

namespace Application.ChatEscalationResolutions.UseCase;

public sealed record GetChatEscalationResolutionByIdQuery(Guid Id)
    : IRequest<ChatEscalationResolutionEntity?>;

public sealed class GetChatEscalationResolutionByIdQueryHandler
    : IRequestHandler<GetChatEscalationResolutionByIdQuery, ChatEscalationResolutionEntity?>
{
    private readonly IUnitOfWork _uow;

    public GetChatEscalationResolutionByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<ChatEscalationResolutionEntity?> Handle(
        GetChatEscalationResolutionByIdQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatEscalationResolutionsRepository.GetByIdAsync(request.Id, cancellationToken);
}
