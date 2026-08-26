using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Pets.Entities;
using MediatR;

namespace Application.Pets.UseCases;

public sealed record GetPetByIdQuery(Guid Id) : IRequest<PetEntity>;

public sealed class GetPetByIdQueryHandler : IRequestHandler<GetPetByIdQuery, PetEntity>
{
    private readonly IUnitOfWork _uow;

    public GetPetByIdQueryHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PetEntity> Handle(GetPetByIdQuery request, CancellationToken cancellationToken)
    {
        var pet = await _uow.PetsRepository.GetByIdAsync(request.Id, cancellationToken);

        if (pet is null)
            throw new NotFoundException("Mascota no encontrada.");

        return pet;
    }
}
