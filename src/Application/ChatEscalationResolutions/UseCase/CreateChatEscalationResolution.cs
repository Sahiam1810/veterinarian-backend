using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatEscalationResolutionEntity = Domain.ChatEscalationResolutions.Entities.ChatEscalationResolution;

namespace Application.ChatEscalationResolutions.UseCase;

public sealed record CreateChatEscalationResolutionCommand(
    Guid ChatEscalationId,
    Guid? ResolvedBy,
    string? ResolutionNote,
    DateTime? ResolvedAt) : IRequest<ChatEscalationResolutionEntity>;

public sealed class CreateChatEscalationResolutionCommandHandler
    : IRequestHandler<CreateChatEscalationResolutionCommand, ChatEscalationResolutionEntity>
{
    private readonly IUnitOfWork _uow;

    public CreateChatEscalationResolutionCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatEscalationResolutionEntity> Handle(
        CreateChatEscalationResolutionCommand request,
        CancellationToken cancellationToken)
    {
        var escalation = await _uow.ChatEscalationsRepository.GetByIdAsync(
            request.ChatEscalationId,
            cancellationToken);
        if (escalation is null)
        {
            throw new NotFoundException(
                $"No se encontró el escalamiento '{request.ChatEscalationId}'.");
        }

        var resolution = ChatEscalationResolutionEntity.Create(
            request.ChatEscalationId,
            request.ResolvedBy,
            request.ResolutionNote,
            request.ResolvedAt);

        await _uow.ChatEscalationResolutionsRepository.AddAsync(resolution, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return resolution;
    }
}
