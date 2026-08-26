using Application.Common.Abstractions;
using MediatR;

namespace Application.UserTokens.UseCase;

public sealed class DeleteUserTokenCommandHandler
    : IRequestHandler<DeleteUserTokenCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public DeleteUserTokenCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(
        DeleteUserTokenCommand request,
        CancellationToken cancellationToken)
    {
        var token = await _uow.UserTokensRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (token is null)
        {
            return false;
        }

        await _uow.UserTokensRepository.DeleteAsync(
            token,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
