using Application.AiModels.Abstraction;
using Application.Common.Exceptions;
using MediatR;
using AiModelEntity = Domain.AiModels.Entities.AiModel;

namespace Application.AiModels.UseCase;

public sealed record ActivateAiModelCommand(Guid Id) : IRequest<AiModelEntity>;

public sealed class ActivateAiModelCommandHandler
    : IRequestHandler<ActivateAiModelCommand, AiModelEntity>
{
    private readonly IAiModelRepository _repository;

    public ActivateAiModelCommandHandler(IAiModelRepository repository)
    {
        _repository = repository;
    }

    public async Task<AiModelEntity> Handle(
        ActivateAiModelCommand request,
        CancellationToken cancellationToken)
    {
        var model = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"No se encontró el modelo de IA '{request.Id}'.");

        model.Activate();

        await _repository.UpdateAsync(model, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return model;
    }
}
