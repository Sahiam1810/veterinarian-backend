using Application.Common.Abstractions;
using Domain.Pets.Entities;
using MediatR;

namespace Application.Pets.UseCases;

public sealed record GetAllPetsQuery() : IRequest<IReadOnlyCollection<PetEntity>>;

public sealed class GetAllPetsQueryHandler : IRequestHandler<GetAllPetsQuery, IReadOnlyCollection<PetEntity>>
{
    private readonly IUnitOfWork _uow;

    public GetAllPetsQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyCollection<PetEntity>> Handle(GetAllPetsQuery request, CancellationToken cancellationToken)
    {
        return await _uow.PetsRepository.GetAllAsync(cancellationToken);
    }
}
