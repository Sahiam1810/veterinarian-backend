using Application.Common.Abstractions;
using Domain.MessageTypes.Entities;
using MediatR;

namespace Application.MessageTypes.UseCases;

public sealed record GetAllMessageTypesQuery() : IRequest<IReadOnlyCollection<MessageTypeEntity>>;

public sealed class GetAllMessageTypesQueryHandler : IRequestHandler<GetAllMessageTypesQuery, IReadOnlyCollection<MessageTypeEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetAllMessageTypesQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyCollection<MessageTypeEntity>> Handle(GetAllMessageTypesQuery request, CancellationToken cancellationToken)
    {
        return await _uow.MessageTypesRepository.GetAllAsync(cancellationToken);
    }
}
