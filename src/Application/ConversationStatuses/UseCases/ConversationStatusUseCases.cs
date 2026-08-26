using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.ConversationStatuses.Entities;
using MediatR;

namespace Application.ConversationStatuses.UseCases;

public sealed record CreateConversationStatusCommand(string Name) : IRequest<Guid>;
public sealed record GetAllConversationStatusesQuery : IRequest<IReadOnlyCollection<ConversationStatusEntity>>;
public sealed record GetConversationStatusByIdQuery(Guid Id) : IRequest<ConversationStatusEntity>;
public sealed record UpdateConversationStatusCommand(Guid Id, string Name) : IRequest;
public sealed record DeleteConversationStatusCommand(Guid Id) : IRequest;

// Crea un estado de conversación.
public sealed class CreateConversationStatusCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<CreateConversationStatusCommand, Guid>
{
    public async Task<Guid> Handle(CreateConversationStatusCommand request, CancellationToken cancellationToken)
    {
        var conversationStatus = new ConversationStatusEntity(request.Name);
        await unitOfWork.ConversationStatusesRepository.AddAsync(conversationStatus, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return conversationStatus.Id;
    }
}

// Lista todos los estados de conversación.
public sealed class GetAllConversationStatusesQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetAllConversationStatusesQuery, IReadOnlyCollection<ConversationStatusEntity>>
{
    public Task<IReadOnlyCollection<ConversationStatusEntity>> Handle(
        GetAllConversationStatusesQuery request,
        CancellationToken cancellationToken) =>
        unitOfWork.ConversationStatusesRepository.GetAllAsync(cancellationToken);
}

// Obtiene un estado de conversación por id.
public sealed class GetConversationStatusByIdQueryHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<GetConversationStatusByIdQuery, ConversationStatusEntity>
{
    public async Task<ConversationStatusEntity> Handle(
        GetConversationStatusByIdQuery request,
        CancellationToken cancellationToken) =>
        await unitOfWork.ConversationStatusesRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Estado de conversación no encontrado.");
}

// Actualiza un estado de conversación.
public sealed class UpdateConversationStatusCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UpdateConversationStatusCommand>
{
    public async Task Handle(UpdateConversationStatusCommand request, CancellationToken cancellationToken)
    {
        var conversationStatus = await unitOfWork.ConversationStatusesRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Estado de conversación no encontrado.");

        conversationStatus.Update(request.Name);
        await unitOfWork.ConversationStatusesRepository.UpdateAsync(conversationStatus, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

// Elimina un estado de conversación.
public sealed class DeleteConversationStatusCommandHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteConversationStatusCommand>
{
    public async Task Handle(DeleteConversationStatusCommand request, CancellationToken cancellationToken)
    {
        var conversationStatus = await unitOfWork.ConversationStatusesRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Estado de conversación no encontrado.");

        await unitOfWork.ConversationStatusesRepository.DeleteAsync(conversationStatus, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
