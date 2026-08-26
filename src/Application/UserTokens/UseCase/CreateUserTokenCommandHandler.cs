using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;
using UserTokenEntity = Domain.UserTokens.Entities.UserTokens;

namespace Application.UserTokens.UseCase;

public sealed class CreateUserTokenCommandHandler
    : IRequestHandler<CreateUserTokenCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateUserTokenCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(
        CreateUserTokenCommand request,
        CancellationToken cancellationToken)
    {
        var account = await _uow.UserAccountsRepository.GetByIdAsync(
            request.AccountId,
            cancellationToken);

        if (account is null)
        {
            throw new NotFoundException(
                "La cuenta especificada no existe.");
        }

        var token = new UserTokenEntity(
            request.AccountId,
            request.TokenValue,
            request.TokenType,
            request.ExpiresAt);

        await _uow.UserTokensRepository.AddAsync(
            token,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return token.Id;
    }
}
