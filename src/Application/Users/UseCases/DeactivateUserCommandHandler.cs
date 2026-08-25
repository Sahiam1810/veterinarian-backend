using Application.Common.Abstractions;
using MediatR;

namespace Application.Users.UseCase;

public sealed class DeactivateUserCommandHandler
    : IRequestHandler<DeactivateUserCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public DeactivateUserCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(
        DeactivateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _uow.UsersRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (user is null)
        {
            return false;
        }

        user.Deactivate();

        await _uow.UsersRepository.UpdateAsync(
            user,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
