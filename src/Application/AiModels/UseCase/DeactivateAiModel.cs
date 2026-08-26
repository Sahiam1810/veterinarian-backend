using Application.AiModels.Abstraction;
using Application.Common.Exceptions;
using MediatR;
using AiModelEntity = Domain.AiModels.Entities.AiModel;

namespace Application.AiModels.UseCase;

public sealed record DeactivateAiModelCommand(Guid Id) : IRequest<AiModelEntity>;

public sealed class DeactivateAiModelCommandHandler
    : IRequestHandler<DeactivateAiModelCommand, AiModelEntity>
{
    private readonly IAiModelRepository _repository;

    public DeactivateAiModelCommandHandler(IAiModelRepository repository)
    {
        _repository = repository;
    }

    public async Task<AiModelEntity> Handle(
        DeactivateAiModelCommand request,
        CancellationToken cancellationToken)
    {
        var model = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el modelo de IA '{request.Id}'.");

        model.Deactivate();

        await _repository.UpdateAsync(model, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return model;
    }
}
