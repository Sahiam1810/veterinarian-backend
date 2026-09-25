using Domain.Common;
using Domain.Clients.ValueObjects;

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

    public ClientFullName FullName { get; private set; } = null!;

    public ClientEmail Email { get; private set; } = null!;

    public ClientIdentificationNumber IdentificationNumber { get; private set; } = null!;

    public ClientAddress Address { get; private set; } = null!;

    // Contacto general del cliente; no sustituye RequesterPhoneNumber de la cita.
    // Nullable solo para lectura: histórico inválido en BD se materializa como null.
    public ClientPhoneNumber? PhoneNumber { get; private set; }

    public bool IsActive { get; private set; }

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
