using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.ClientsPets.Entities;
using MediatR;

namespace Application.ClientsPets.UseCases;

public sealed record CreateClientPetCommand(Guid ClientId, Guid PetId, bool IsPrimaryOwner) : IRequest<Guid>;
public sealed record GetAllClientPetsQuery : IRequest<IReadOnlyCollection<ClientPetEntity>>;
public sealed record GetClientPetByIdQuery(Guid Id) : IRequest<ClientPetEntity>;
public sealed record UpdateClientPetCommand(Guid Id, bool IsPrimaryOwner) : IRequest;
public sealed record DeleteClientPetCommand(Guid Id) : IRequest;

public sealed class CreateClientPetCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateClientPetCommand, Guid>
{
    public async Task<Guid> Handle(CreateClientPetCommand request, CancellationToken cancellationToken)
    {
        var client = await unitOfWork.ClientsRepository.GetByIdAsync(request.ClientId, cancellationToken) ?? throw new NotFoundException("Cliente no encontrado.");
        var pet = await unitOfWork.PetsRepository.GetByIdAsync(request.PetId, cancellationToken) ?? throw new NotFoundException("Mascota no encontrada.");
        if (await unitOfWork.ClientPetsRepository.ExistsByClientAndPetAsync(request.ClientId, request.PetId, cancellationToken))
            throw new ConflictException("La mascota ya está asociada a este cliente.");
        var clientPet = new ClientPetEntity(client, pet, request.IsPrimaryOwner);
        await unitOfWork.ClientPetsRepository.AddAsync(clientPet, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return clientPet.Id;
    }
}
public sealed class GetAllClientPetsQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAllClientPetsQuery, IReadOnlyCollection<ClientPetEntity>>
{
    public Task<IReadOnlyCollection<ClientPetEntity>> Handle(GetAllClientPetsQuery request, CancellationToken cancellationToken) => unitOfWork.ClientPetsRepository.GetAllAsync(cancellationToken);
}
public sealed class GetClientPetByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetClientPetByIdQuery, ClientPetEntity>
{
    public async Task<ClientPetEntity> Handle(GetClientPetByIdQuery request, CancellationToken cancellationToken) =>
        await unitOfWork.ClientPetsRepository.GetByIdAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Relación cliente-mascota no encontrada.");
}
public sealed class UpdateClientPetCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateClientPetCommand>
{
    public async Task Handle(UpdateClientPetCommand request, CancellationToken cancellationToken)
    {
        var clientPet = await unitOfWork.ClientPetsRepository.GetByIdAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Relación cliente-mascota no encontrada.");
        clientPet.Update(request.IsPrimaryOwner);
        await unitOfWork.ClientPetsRepository.UpdateAsync(clientPet, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
public sealed class DeleteClientPetCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteClientPetCommand>
{
    public async Task Handle(DeleteClientPetCommand request, CancellationToken cancellationToken)
    {
        var clientPet = await unitOfWork.ClientPetsRepository.GetByIdAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Relación cliente-mascota no encontrada.");
        await unitOfWork.ClientPetsRepository.DeleteAsync(clientPet, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
