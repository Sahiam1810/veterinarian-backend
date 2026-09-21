using Application.Clients.Errors;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Clients.Entities;
using Domain.Clients.ValueObjects;
using MediatR;

namespace Application.Clients.UseCases;

public sealed record CreateClientCommand(
    string FullName,
    string Email,
    string IdentificationNumber,
    string? PhoneNumber = null,
    string? Address = null) : IRequest<Guid>;

public sealed class CreateClientCommandHandler : IRequestHandler<CreateClientCommand, Guid>
{
    private readonly IUnitOfWork _uow;

    public CreateClientCommandHandler(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Guid> Handle(CreateClientCommand request, CancellationToken cancellationToken)
    {
        var exists = await _uow.ClientsRepository.ExistsByIdentificationNumberAsync(
            request.IdentificationNumber,
            cancellationToken);

        if (exists)
        {
            throw new ConflictException(
                "Ya existe un cliente con ese número de identificación.",
                ClientErrorCodes.IdentificationAlreadyInUse);
        }

        var emailInUse = await _uow.ClientsRepository.ExistsByEmailAsync(
            request.Email,
            cancellationToken);

        if (emailInUse)
        {
            throw new ConflictException(
                "Ya existe un cliente con ese correo electrónico.",
                ClientErrorCodes.EmailAlreadyInUse);
        }

        // Normaliza y persiste dígitos; unicidad en app + índice UX_CLIENTS_PHONE_NUMBER en BD.
        var phoneNumber = ClientPhoneNumber.Create(request.PhoneNumber);
        var phoneInUse = await _uow.ClientsRepository.ExistsByPhoneAsync(
            phoneNumber.Value,
            cancellationToken);

        if (phoneInUse)
        {
            throw new ConflictException(
                "Ya existe un cliente con ese número de teléfono.",
                ClientErrorCodes.PhoneAlreadyInUse);
        }

        var client = new ClientEntity(
            request.FullName,
            request.Email,
            request.IdentificationNumber,
            phoneNumber.Value,
            request.Address);

        await _uow.ClientsRepository.AddAsync(client, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return client.Id;
    }
}
