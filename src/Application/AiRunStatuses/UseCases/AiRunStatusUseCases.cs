using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.AiRunStatuses.Entities;
using MediatR;

namespace Application.AiRunStatuses.UseCases;

public sealed record CreateAiRunStatusCommand(string NameStatus) : IRequest<Guid>;
public sealed record GetAllAiRunStatusesQuery : IRequest<IReadOnlyCollection<AiRunStatusEntity>>;
public sealed record GetAiRunStatusByIdQuery(Guid Id) : IRequest<AiRunStatusEntity>;
public sealed record UpdateAiRunStatusCommand(Guid Id, string NameStatus) : IRequest;
public sealed record DeleteAiRunStatusCommand(Guid Id) : IRequest;

public sealed class CreateAiRunStatusCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateAiRunStatusCommand, Guid>
{
    public async Task<Guid> Handle(CreateAiRunStatusCommand request, CancellationToken cancellationToken)
    {
        var aiRunStatus = new AiRunStatusEntity(request.NameStatus);
        await unitOfWork.AiRunStatusesRepository.AddAsync(aiRunStatus, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return aiRunStatus.Id;
    }
}

public sealed class GetAllAiRunStatusesQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAllAiRunStatusesQuery, IReadOnlyCollection<AiRunStatusEntity>>
{
    public Task<IReadOnlyCollection<AiRunStatusEntity>> Handle(GetAllAiRunStatusesQuery request, CancellationToken cancellationToken) => unitOfWork.AiRunStatusesRepository.GetAllAsync(cancellationToken);
}

public sealed class GetAiRunStatusByIdQueryHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAiRunStatusByIdQuery, AiRunStatusEntity>
{
    public async Task<AiRunStatusEntity> Handle(GetAiRunStatusByIdQuery request, CancellationToken cancellationToken) =>
        await unitOfWork.AiRunStatusesRepository.GetByIdAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Estado de ejecución AI no encontrado.");
}

public sealed class UpdateAiRunStatusCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateAiRunStatusCommand>
{
    public async Task Handle(UpdateAiRunStatusCommand request, CancellationToken cancellationToken)
    {
        var aiRunStatus = await unitOfWork.AiRunStatusesRepository.GetByIdAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Estado de ejecución AI no encontrado.");
        aiRunStatus.Update(request.NameStatus);
        await unitOfWork.AiRunStatusesRepository.UpdateAsync(aiRunStatus, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public sealed class DeleteAiRunStatusCommandHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteAiRunStatusCommand>
{
    public async Task Handle(DeleteAiRunStatusCommand request, CancellationToken cancellationToken)
    {
        var aiRunStatus = await unitOfWork.AiRunStatusesRepository.GetByIdAsync(request.Id, cancellationToken) ?? throw new NotFoundException("Estado de ejecución AI no encontrado.");
        await unitOfWork.AiRunStatusesRepository.DeleteAsync(aiRunStatus, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
