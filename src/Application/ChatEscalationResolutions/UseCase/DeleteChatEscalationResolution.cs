using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.ChatEscalationResolutions.UseCase;

public sealed record DeleteChatEscalationResolutionCommand(Guid Id) : IRequest;

public sealed class DeleteChatEscalationResolutionCommandHandler
    : IRequestHandler<DeleteChatEscalationResolutionCommand>
{
    private readonly IUnitOfWork _uow;

    public DeleteChatEscalationResolutionCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(
        DeleteChatEscalationResolutionCommand request,
        CancellationToken cancellationToken)
    {
        var resolution = await _uow.ChatEscalationResolutionsRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException(
                $"No se encontró la resolución '{request.Id}'.");

        await _uow.ChatEscalationResolutionsRepository.DeleteAsync(resolution, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
