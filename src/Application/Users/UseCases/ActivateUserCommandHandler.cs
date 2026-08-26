using Application.Common.Abstractions;
using MediatR;

namespace Application.Users.UseCase;

public sealed class ActivateUserCommandHandler
    : IRequestHandler<ActivateUserCommand, bool>
{
    private readonly IUnitOfWork _uow;

    public ActivateUserCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<bool> Handle(
        ActivateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _uow.UsersRepository.GetByIdAsync(
            request.Id,
            cancellationToken);

        if (user is null)
        {
            return false;
        }

        user.Activate();

        await _uow.UsersRepository.UpdateAsync(
            user,
            cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);

        return true;
    }
}
