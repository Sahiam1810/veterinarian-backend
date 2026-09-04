using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Application.Clients.Errors;
using Domain.Clients.ValueObjects;
using MediatR;

namespace Application.Clients.UseCases;

public sealed record UpdateClientCommand(
    Guid Id,
    Guid UserId,
    string IdentificationNumber,
    string? Address,
    DateTime? RegistrationDate = null,
    string? PhoneNumber = null) : IRequest;

public sealed class UpdateClientCommandHandler : IRequestHandler<UpdateClientCommand>
{
    private readonly IUnitOfWork _uow;

    public UpdateClientCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task Handle(UpdateClientCommand request, CancellationToken cancellationToken)
    {
        var client = await _uow.ClientsRepository.GetByIdAsync(request.Id, cancellationToken);
        if (client is null)
        {
            throw new NotFoundException("Cliente no encontrado.");
        }

        var user = await _uow.UsersRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException("Usuario no encontrado.");
        }

        var exists = await _uow.ClientsRepository.ExistsByIdentificationNumberAsync(
            request.IdentificationNumber,
            cancellationToken,
            request.Id);

        if (exists)
        {
            throw new ConflictException("Ya existe otro cliente con ese número de identificación.");
        }

        var userAlreadyHasClient = await _uow.ClientsRepository.ExistsByUserIdAsync(
            request.UserId,
            cancellationToken,
            request.Id);

        if (userAlreadyHasClient)
        {
            throw new ConflictException("Ese usuario ya tiene otro perfil de cliente asociado.");
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            var normalized = ClientPhoneNumber.Create(request.PhoneNumber).Value;
            var phoneInUse = await _uow.ClientsRepository.ExistsByPhoneAsync(
                normalized,
                cancellationToken,
                request.Id);

            if (phoneInUse)
            {
                throw new ConflictException(
                    "Ya existe un cliente con ese número de teléfono.",
                    ClientErrorCodes.PhoneAlreadyInUse);
            }
        }

        client.Update(
            request.UserId,
            request.IdentificationNumber,
            request.Address,
            request.RegistrationDate,
            request.PhoneNumber);

        await _uow.ClientsRepository.UpdateAsync(client, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
