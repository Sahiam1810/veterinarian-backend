using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Clients.Entities;
using Domain.Clients.ValueObjects;
using Domain.Roles;
using Domain.Veterinarians.Entities;
using MediatR;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Users.UseCase;

public sealed class CreateUserCommandHandler
    : IRequestHandler<CreateUserCommand, Guid>
{
    private const string ClientRoleName = "Cliente";
    private const string VeterinarianRoleName = "Veterinario";

    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserCommandHandler(
        IUnitOfWork uow,
        IPasswordHasher passwordHasher)
    {
        _uow = uow;
        _passwordHasher = passwordHasher;
    }

    public async Task<Guid> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        if (SystemRoles.IsSuperAdmin(request.RoleId))
        {
            throw new ForbiddenException(
                "El rol SuperAdmin solo se asigna mediante el proceso seguro de aprovisionamiento.");
        }

        var role = await _uow.RolesRepository.GetByIdAsync(
            request.RoleId,
            cancellationToken);

        if (role is null)
        {
            throw new NotFoundException(
                "El rol especificado no existe.");
        }

        var emailInUse = await _uow.UsersRepository.ExistsByEmailAsync(
            request.Email,
            cancellationToken);

        if (emailInUse)
        {
            throw new ConflictException(
                "Ya existe un usuario con ese correo electrónico.");
        }

        // Cliente nunca se loguea (solo interactúa vía chatbot): sin contraseña.
        var isClientRole = string.Equals(role.Name.Value, ClientRoleName, StringComparison.Ordinal);
        var isVeterinarianRole = string.Equals(role.Name.Value, VeterinarianRoleName, StringComparison.Ordinal);
        var passwordHash = isClientRole ? null : _passwordHasher.Hash(request.Password!);

        Guid createdUserId = Guid.Empty;

        await _uow.ExecuteInTransactionAsync(async ct =>
        {
            var user = new UserEntity(
                request.FullName,
                request.Email,
                passwordHash,
                request.RoleId);

            await _uow.UsersRepository.AddAsync(user, ct);
            await _uow.SaveChangesAsync(ct);
            createdUserId = user.Id;

            if (isClientRole)
            {
                await EnsureClientProfileAsync(user.Id, request, ct);
            }
            else if (isVeterinarianRole)
            {
                await EnsureVeterinarianProfileAsync(user.Id, request, ct);
            }
        }, cancellationToken);

        return createdUserId;
    }

    // Crea la fila Clients en el alta del usuario (evita auto-sync en pantallas de lectura).
    private async Task EnsureClientProfileAsync(
        Guid userId,
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        if (await _uow.ClientsRepository.ExistsByUserIdAsync(userId, cancellationToken))
        {
            return;
        }

        var identification = string.IsNullOrWhiteSpace(request.ClientIdentificationNumber)
            ? BuildPlaceholderIdentification(userId)
            : request.ClientIdentificationNumber.Trim();

        var phone = string.IsNullOrWhiteSpace(request.ClientPhoneNumber)
            ? BuildPlaceholderPhone(userId)
            : ClientPhoneNumber.Normalize(request.ClientPhoneNumber);

        if (await _uow.ClientsRepository.ExistsByIdentificationNumberAsync(identification, cancellationToken))
        {
            identification = BuildPlaceholderIdentification(userId);
        }

        if (await _uow.ClientsRepository.ExistsByPhoneAsync(phone, cancellationToken))
        {
            phone = BuildPlaceholderPhone(userId);
        }

        var client = new ClientEntity(
            userId,
            identification,
            request.ClientAddress,
            phoneNumber: phone);

        await _uow.ClientsRepository.AddAsync(client, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    // Crea la fila Veterinarians en el alta del usuario con especialidad/licencia dadas o por defecto.
    private async Task EnsureVeterinarianProfileAsync(
        Guid userId,
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        if (await _uow.VeterinariansRepository.ExistsByUserIdAsync(userId, cancellationToken))
        {
            return;
        }

        var specialtyId = request.SpecialtyId;
        if (!specialtyId.HasValue || specialtyId.Value == Guid.Empty)
        {
            var specialties = await _uow.SpecialtiesRepository.GetAllAsync(cancellationToken);
            specialtyId = specialties.FirstOrDefault()?.Id;
        }
        else
        {
            var specialty = await _uow.SpecialtiesRepository.GetByIdAsync(specialtyId.Value, cancellationToken);
            if (specialty is null)
            {
                throw new NotFoundException("La especialidad especificada no existe.");
            }
        }

        if (!specialtyId.HasValue || specialtyId.Value == Guid.Empty)
        {
            throw new ConflictException(
                "No hay especialidades configuradas. Configúralas antes de registrar un veterinario.");
        }

        var licenseNumber = string.IsNullOrWhiteSpace(request.LicenseNumber)
            ? BuildPlaceholderLicense(userId)
            : request.LicenseNumber.Trim();

        if (await _uow.VeterinariansRepository.ExistsByLicenseNumberAsync(licenseNumber, cancellationToken))
        {
            licenseNumber = BuildPlaceholderLicense(userId);
        }

        var veterinarian = new Veterinarian(userId, specialtyId.Value, licenseNumber);
        await _uow.VeterinariansRepository.AddAsync(veterinarian, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    // Identificación única derivada del Guid (máx. 20).
    private static string BuildPlaceholderIdentification(Guid userId)
        => $"DOC-{userId:N}"[..20];

    // Teléfono de 10 dígitos único por usuario (evita choque con DOC-PENDIENTE/0000000000).
    private static string BuildPlaceholderPhone(Guid userId)
    {
        var n = BitConverter.ToUInt64(userId.ToByteArray(), 0);
        return (n % 10_000_000_000UL).ToString("D10");
    }

    // Licencia placeholder única (máx. 20).
    private static string BuildPlaceholderLicense(Guid userId)
        => $"LIC-{userId:N}"[..12].ToUpperInvariant();
}
