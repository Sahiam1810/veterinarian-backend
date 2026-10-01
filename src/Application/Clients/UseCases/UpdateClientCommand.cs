using Application.Clients.Errors;
using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Clients.ValueObjects;
using MediatR;

namespace Application.Clients.UseCases;

public sealed record UpdateClientCommand(
    Guid Id,
    string FullName,
    string Email,
    string IdentificationNumber,
    string PhoneNumber,
    string? Address,
    bool IsActive) : IRequest;

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

        var exists = await _uow.ClientsRepository.ExistsByIdentificationNumberAsync(
            request.IdentificationNumber,
            cancellationToken,
            request.Id);

        if (exists)
        {
            throw new ConflictException(
                "Ya existe otro cliente con ese número de identificación.",
                ClientErrorCodes.IdentificationAlreadyInUse);
        }

        var emailInUse = await _uow.ClientsRepository.ExistsByEmailAsync(
            request.Email,
            cancellationToken,
            request.Id);

        if (emailInUse)
        {
            throw new ConflictException(
                "Ya existe otro cliente con ese correo electrónico.",
                ClientErrorCodes.EmailAlreadyInUse);
        }

        // Update no deja el teléfono vacío: Create exige dígitos válidos.
        var phoneNumber = ClientPhoneNumber.Create(request.PhoneNumber);
        var phoneInUse = await _uow.ClientsRepository.ExistsByPhoneAsync(
            phoneNumber.Value,
            cancellationToken,
            request.Id);

        if (phoneInUse)
        {
            throw new ConflictException(
                "Ya existe un cliente con ese número de teléfono.",
                ClientErrorCodes.PhoneAlreadyInUse);
        }

        client.Update(
            request.FullName,
            request.Email,
            request.IdentificationNumber,
            phoneNumber.Value,
            request.Address);

        if (request.IsActive)
        {
            client.Activate();
        }
        else
        {
            client.Deactivate();
        }

        await _uow.ClientsRepository.UpdateAsync(client, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
