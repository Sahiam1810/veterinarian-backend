using Domain.Common;
using Domain.Clients.ValueObjects;
using UserEntity = Domain.Users.Entities.Users;

namespace Domain.Clients.Entities;

public sealed class ClientEntity : BaseEntity<Guid>
{
    private ClientEntity()
    {
    }

    public ClientEntity(
        string fullName,
        string email,
        string identificationNumber,
        string phoneNumber,
        string? address)
    {
        Id = Guid.NewGuid();
        FullName = ClientFullName.Create(fullName);
        Email = ClientEmail.Create(email);
        IdentificationNumber = ClientIdentificationNumber.Create(identificationNumber);
        Address = ClientAddress.Create(address);
        PhoneNumber = ClientPhoneNumber.Create(phoneNumber);
        IsActive = true;
    }

    // Vestigio de la separación clientes/usuarios: opcional hasta que la T11 lo elimine.
    public Guid? UserId { get; private set; }

    public ClientFullName FullName { get; private set; } = null!;

    public ClientEmail Email { get; private set; } = null!;

    public ClientIdentificationNumber IdentificationNumber { get; private set; } = null!;

    public ClientAddress Address { get; private set; } = null!;

    // Contacto general del cliente; no sustituye RequesterPhoneNumber de la cita.
    public ClientPhoneNumber PhoneNumber { get; private set; } = null!;

    public bool IsActive { get; private set; }

    // Navigation property
    public UserEntity? User { get; private set; }

    public void Update(
        string fullName,
        string email,
        string identificationNumber,
        string phoneNumber,
        string? address)
    {
        FullName = ClientFullName.Create(fullName);
        Email = ClientEmail.Create(email);
        IdentificationNumber = ClientIdentificationNumber.Create(identificationNumber);
        Address = ClientAddress.Create(address);
        PhoneNumber = ClientPhoneNumber.Create(phoneNumber);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
