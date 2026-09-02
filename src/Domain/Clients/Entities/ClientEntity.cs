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
        Guid userId,
        string identificationNumber,
        string? address,
        DateTime? registrationDate = null,
        string? phoneNumber = null)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        IdentificationNumber = ClientIdentificationNumber.Create(identificationNumber);
        Address = ClientAddress.Create(address);
        PhoneNumber = ClientPhoneNumber.CreateOptional(phoneNumber);
        RegistrationDate = registrationDate ?? DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }

    public ClientIdentificationNumber IdentificationNumber { get; private set; } = null!;

    public ClientAddress Address { get; private set; } = null!;

    // Contacto general del cliente; no sustituye RequesterPhoneNumber de la cita.
    public ClientPhoneNumber? PhoneNumber { get; private set; }

    public DateTime RegistrationDate { get; private set; }

    // Navigation property
    public UserEntity? User { get; private set; }

    public void Update(
        Guid userId,
        string identificationNumber,
        string? address,
        DateTime? registrationDate = null,
        string? phoneNumber = null)
    {
        UserId = userId;
        IdentificationNumber = ClientIdentificationNumber.Create(identificationNumber);
        Address = ClientAddress.Create(address);
        PhoneNumber = ClientPhoneNumber.CreateOptional(phoneNumber);
        if (registrationDate.HasValue)
        {
            RegistrationDate = registrationDate.Value;
        }
        UpdatedAt = DateTime.UtcNow;
    }
}
