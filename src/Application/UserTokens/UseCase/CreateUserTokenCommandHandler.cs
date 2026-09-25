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
        var user = await _uow.UsersRepository.GetByIdAsync(
            request.UserId,
            cancellationToken);

        if (user is null)
        {
            throw new NotFoundException(
                "El usuario especificado no existe.");
        }

        var token = new UserTokenEntity(
            request.UserId,
            request.TokenValue,
            request.TokenType,
            request.ExpiresAt,
            DateTime.UtcNow);

        await _uow.UserTokensRepository.AddAsync(
            token,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return token.Id;
    }
}
