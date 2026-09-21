using Application.Common.Abstractions;
using Application.Common.Exceptions;
using Domain.Roles;
using Domain.Veterinarians.Entities;
using MediatR;
using UserEntity = Domain.Users.Entities.Users;

namespace Application.Users.UseCase;

public sealed class CreateUserCommandHandler
    : IRequestHandler<CreateUserCommand, Guid>
{
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
                "El rol SuperAdmin solo se asigna mediante el flujo seguro de aprovisionamiento.");
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

        var isVeterinarianRole = string.Equals(role.Name.Value, VeterinarianRoleName, StringComparison.Ordinal);
        var passwordHash = _passwordHasher.Hash(request.Password);

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

            if (isVeterinarianRole)
            {
                await EnsureVeterinarianProfileAsync(user.Id, request, ct);
            }
        }, cancellationToken);

        return createdUserId;
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

    // Licencia placeholder única (máx. 20).
    private static string BuildPlaceholderLicense(Guid userId)
        => $"LIC-{userId:N}"[..12].ToUpperInvariant();
}
