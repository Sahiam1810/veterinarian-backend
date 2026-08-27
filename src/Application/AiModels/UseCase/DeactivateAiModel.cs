using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using AiModelEntity = Domain.AiModels.Entities.AiModel;

namespace Application.AiModels.UseCase;

public sealed record DeactivateAiModelCommand(Guid Id) : IRequest<AiModelEntity>;

public sealed class DeactivateAiModelCommandHandler
    : IRequestHandler<DeactivateAiModelCommand, AiModelEntity>
{
    private readonly IUnitOfWork _uow;

    public DeactivateAiModelCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<AiModelEntity> Handle(
        DeactivateAiModelCommand request,
        CancellationToken cancellationToken)
    {
        var model = await _uow.AiModelsRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el modelo de IA '{request.Id}'.");

        model.Deactivate();

        await _uow.AiModelsRepository.UpdateAsync(model, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return model;
    }
}
