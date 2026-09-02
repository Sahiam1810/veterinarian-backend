using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Clients.Entities;
using MediatR;

namespace Application.Clients.UseCases;

public sealed record CreateClientCommand(
    Guid UserId,
    string IdentificationNumber,
    string? Address,
    DateTime? RegistrationDate = null,
    string? PhoneNumber = null) : IRequest<Guid>;

public sealed class CreateClientCommandHandler : IRequestHandler<CreateClientCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateClientCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateClientCommand request, CancellationToken cancellationToken)
    {
        var user = await _uow.UsersRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException("Usuario no encontrado.");
        }

        var exists = await _uow.ClientsRepository.ExistsByIdentificationNumberAsync(
            request.IdentificationNumber,
            cancellationToken);

        if (exists)
        {
            throw new ConflictException("Ya existe un cliente con ese número de identificación.");
        }

        var client = new ClientEntity(
            request.UserId,
            request.IdentificationNumber,
            request.Address,
            request.RegistrationDate,
            request.PhoneNumber);

        await _uow.ClientsRepository.AddAsync(client, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return client.Id;
    }
}
