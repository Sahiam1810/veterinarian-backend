using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Pets.UseCases;

public sealed record DeletePetCommand(Guid Id) : IRequest;

public sealed class DeletePetCommandHandler : IRequestHandler<DeletePetCommand>
{
    private readonly IUnitOfWork _uow;

    public DeletePetCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(DeletePetCommand request, CancellationToken cancellationToken)
    {
        var pet = await _uow.PetsRepository.GetByIdAsync(request.Id, cancellationToken);

        if (pet is null)
            throw new NotFoundException("Mascota no encontrada.");

        await _uow.PetsRepository.DeleteAsync(pet, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
