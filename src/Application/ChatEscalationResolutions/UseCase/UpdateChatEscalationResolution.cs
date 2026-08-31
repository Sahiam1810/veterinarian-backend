using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using ChatEscalationResolutionEntity = Domain.ChatEscalationResolutions.Entities.ChatEscalationResolution;

namespace Application.ChatEscalationResolutions.UseCase;

public sealed record UpdateChatEscalationResolutionCommand(
    Guid Id,
    Guid? ResolvedBy,
    string? ResolutionNote,
    DateTime? ResolvedAt) : IRequest<ChatEscalationResolutionEntity>;

public sealed class UpdateChatEscalationResolutionCommandHandler
    : IRequestHandler<UpdateChatEscalationResolutionCommand, ChatEscalationResolutionEntity>
{
    private readonly IUnitOfWork _uow;

    public UpdateChatEscalationResolutionCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatEscalationResolutionEntity> Handle(
        UpdateChatEscalationResolutionCommand request,
        CancellationToken cancellationToken)
    {
        var resolution = await _uow.ChatEscalationResolutionsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException(
                $"No se encontró la resolución '{request.Id}'.");

        resolution.Update(
            request.ResolvedBy,
            request.ResolutionNote,
            request.ResolvedAt);

        await _uow.ChatEscalationResolutionsRepository.UpdateAsync(resolution, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return resolution;
    }
}
