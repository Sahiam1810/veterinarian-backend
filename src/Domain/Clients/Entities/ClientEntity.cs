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
        string? email,
        string? identificationNumber,
        string phoneNumber,
        string? address)
    {
        Id = Guid.NewGuid();
        FullName = ClientFullName.Create(fullName);
        Email = string.IsNullOrWhiteSpace(email) ? null : ClientEmail.Create(email);
        IdentificationNumber = string.IsNullOrWhiteSpace(identificationNumber) ? null : ClientIdentificationNumber.Create(identificationNumber);
        Address = ClientAddress.Create(address);
        PhoneNumber = ClientPhoneNumber.Create(phoneNumber);
        IsActive = true;
    }

    public ClientFullName FullName { get; private set; } = null!;

    public ClientEmail? Email { get; private set; }

    public ClientIdentificationNumber? IdentificationNumber { get; private set; }

    public ClientAddress Address { get; private set; } = null!;

    // Contacto general del cliente; no sustituye RequesterPhoneNumber de la cita.
    public ClientPhoneNumber PhoneNumber { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public void Update(
        string fullName,
        string? email,
        string? identificationNumber,
        string phoneNumber,
        string? address)
    {
        FullName = ClientFullName.Create(fullName);
        Email = string.IsNullOrWhiteSpace(email) ? null : ClientEmail.Create(email);
        IdentificationNumber = string.IsNullOrWhiteSpace(identificationNumber) ? null : ClientIdentificationNumber.Create(identificationNumber);
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
