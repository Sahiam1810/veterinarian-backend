using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.UserTokens.UseCase;

public sealed class DeleteUserTokenCommandHandler
    : IRequestHandler<DeleteUserTokenCommand>
{
    private readonly IUnitOfWork _uow;

    public DeleteUserTokenCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(
        DeleteUserTokenCommand request,
        CancellationToken cancellationToken)
    {
        var token = await _uow.UserTokensRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Token no encontrado.");

        await _uow.UserTokensRepository.DeleteAsync(
            token,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
    }
}
