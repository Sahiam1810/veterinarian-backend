using Application.Common.Abstractions;
using Application.Common.Exceptions;
using MediatR;

namespace Application.Users.UseCase;

public sealed class ActivateUserCommandHandler
    : IRequestHandler<ActivateUserCommand>
{
    private readonly IUnitOfWork _uow;

    public ActivateUserCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(
        ActivateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _uow.UsersRepository.GetByIdAsync(
            request.Id,
            cancellationToken)
            ?? throw new NotFoundException("Usuario no encontrado.");

        user.Activate();

        await _uow.UsersRepository.UpdateAsync(
            user,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
    }
}
