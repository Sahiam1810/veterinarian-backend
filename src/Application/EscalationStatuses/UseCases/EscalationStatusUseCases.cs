using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.EscalationStatuses.Entities;
using MediatR;

namespace Application.EscalationStatuses.UseCases;

public sealed record CreateEscalationStatusCommand(string Name) : IRequest<Guid>;
public sealed record GetAllEscalationStatusesQuery : IRequest<IReadOnlyCollection<EscalationStatusEntity>>;
public sealed record GetEscalationStatusByIdQuery(Guid Id) : IRequest<EscalationStatusEntity>;
public sealed record UpdateEscalationStatusCommand(Guid Id, string Name) : IRequest;
public sealed record DeleteEscalationStatusCommand(Guid Id) : IRequest;

// Crea un estado de escalamiento.
public sealed class CreateEscalationStatusCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateEscalationStatusCommand, Guid>
{
    public async Task<Guid> Handle(CreateEscalationStatusCommand request, CancellationToken cancellationToken)
    {
        var escalationStatus = new EscalationStatusEntity(request.Name);
        await unitOfWork.EscalationStatusesRepository.AddAsync(escalationStatus, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return escalationStatus.Id;
    }
}

// Lista todos los estados de escalamiento.
public sealed class GetAllEscalationStatusesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllEscalationStatusesQuery, IReadOnlyCollection<EscalationStatusEntity>>
{
    public Task<IReadOnlyCollection<EscalationStatusEntity>> Handle(
        GetAllEscalationStatusesQuery request,
        CancellationToken cancellationToken) =>
        unitOfWork.EscalationStatusesRepository.GetAllAsync(cancellationToken);
}

// Obtiene un estado de escalamiento por id.
public sealed class GetEscalationStatusByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetEscalationStatusByIdQuery, EscalationStatusEntity>
{
    public async Task<EscalationStatusEntity> Handle(
        GetEscalationStatusByIdQuery request,
        CancellationToken cancellationToken) =>
        await unitOfWork.EscalationStatusesRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Estado de escalamiento no encontrado.");
}

// Actualiza un estado de escalamiento.
public sealed class UpdateEscalationStatusCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateEscalationStatusCommand>
{
    public async Task Handle(UpdateEscalationStatusCommand request, CancellationToken cancellationToken)
    {
        var escalationStatus = await unitOfWork.EscalationStatusesRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Estado de escalamiento no encontrado.");

        escalationStatus.Update(request.Name);
        await unitOfWork.EscalationStatusesRepository.UpdateAsync(escalationStatus, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

// Elimina un estado de escalamiento.
public sealed class DeleteEscalationStatusCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteEscalationStatusCommand>
{
    public async Task Handle(DeleteEscalationStatusCommand request, CancellationToken cancellationToken)
    {
        var escalationStatus = await unitOfWork.EscalationStatusesRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Estado de escalamiento no encontrado.");

        await unitOfWork.EscalationStatusesRepository.DeleteAsync(escalationStatus, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
