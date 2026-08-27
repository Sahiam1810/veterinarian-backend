using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Priorities.Entities;
using MediatR;

namespace Application.Priorities.UseCases;

public sealed record CreatePriorityCommand(string Name) : IRequest<Guid>;
public sealed record GetAllPrioritiesQuery : IRequest<IReadOnlyCollection<PriorityEntity>>;
public sealed record GetPriorityByIdQuery(Guid Id) : IRequest<PriorityEntity>;
public sealed record UpdatePriorityCommand(Guid Id, string Name) : IRequest;
public sealed record DeletePriorityCommand(Guid Id) : IRequest;

// Crea una prioridad.
public sealed class CreatePriorityCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreatePriorityCommand, Guid>
{
    public async Task<Guid> Handle(CreatePriorityCommand request, CancellationToken cancellationToken)
    {
        var priority = new PriorityEntity(request.Name);
        await unitOfWork.PrioritiesRepository.AddAsync(priority, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return priority.Id;
    }
}

// Lista todas las prioridades.
public sealed class GetAllPrioritiesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllPrioritiesQuery, IReadOnlyCollection<PriorityEntity>>
{
    public Task<IReadOnlyCollection<PriorityEntity>> Handle(
        GetAllPrioritiesQuery request,
        CancellationToken cancellationToken) =>
        unitOfWork.PrioritiesRepository.GetAllAsync(cancellationToken);
}

// Obtiene una prioridad por id.
public sealed class GetPriorityByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetPriorityByIdQuery, PriorityEntity>
{
    public async Task<PriorityEntity> Handle(GetPriorityByIdQuery request, CancellationToken cancellationToken) =>
        await unitOfWork.PrioritiesRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Prioridad no encontrada.");
}

// Actualiza una prioridad.
public sealed class UpdatePriorityCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdatePriorityCommand>
{
    public async Task Handle(UpdatePriorityCommand request, CancellationToken cancellationToken)
    {
        var priority = await unitOfWork.PrioritiesRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Prioridad no encontrada.");

        priority.Update(request.Name);
        await unitOfWork.PrioritiesRepository.UpdateAsync(priority, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

// Elimina una prioridad.
public sealed class DeletePriorityCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeletePriorityCommand>
{
    public async Task Handle(DeletePriorityCommand request, CancellationToken cancellationToken)
    {
        var priority = await unitOfWork.PrioritiesRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Prioridad no encontrada.");

        await unitOfWork.PrioritiesRepository.DeleteAsync(priority, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
