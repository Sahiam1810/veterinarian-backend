using Application.Common.Abstractions;
using MediatR;
using ChatEscalationEntity = Domain.ChatEscalations.Entities.ChatEscalation;

namespace Application.ChatEscalations.UseCase;

public sealed record GetAllChatEscalationsQuery
    : IRequest<IReadOnlyCollection<ChatEscalationEntity>>;

public sealed class GetAllChatEscalationsQueryHandler
    : IRequestHandler<GetAllChatEscalationsQuery, IReadOnlyCollection<ChatEscalationEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetAllChatEscalationsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<IReadOnlyCollection<ChatEscalationEntity>> Handle(
        GetAllChatEscalationsQuery request,
        CancellationToken cancellationToken)
        => _uow.ChatEscalationsRepository.GetAllAsync(cancellationToken);
}
