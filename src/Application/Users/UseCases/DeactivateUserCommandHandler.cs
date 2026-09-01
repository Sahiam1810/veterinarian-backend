using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Users.UseCase;

public sealed class DeactivateUserCommandHandler
    : IRequestHandler<DeactivateUserCommand>
{
    private readonly IUnitOfWork _uow;

    public DeactivateUserCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(
        DeactivateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _uow.UsersRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Usuario no encontrado.");

        user.Deactivate();

        await _uow.UsersRepository.UpdateAsync(
            user,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
    }
}
