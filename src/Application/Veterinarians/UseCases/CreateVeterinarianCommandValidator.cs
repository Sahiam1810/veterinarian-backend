using Application.Common.Abstractions;
using FluentValidation;

namespace Application.Veterinarians.UseCases;

public sealed class CreateVeterinarianCommandValidator : AbstractValidator<CreateVeterinarianCommand>
{
    public CreateVeterinarianCommandValidator(IUnitOfWork unitOfWork)
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("El usuario es requerido.")
            .MustAsync(async (userId, cancellationToken) =>
                !await unitOfWork.VeterinariansRepository.ExistsByUserIdAsync(userId, cancellationToken))
            .WithMessage("Ese usuario ya tiene un perfil de veterinario asociado.");

        RuleFor(x => x.SpecialtyId)
            .NotEmpty().WithMessage("La especialidad es requerida.");

        RuleFor(x => x.LicenseNumber)
            .NotEmpty().WithMessage("El número de tarjeta/licencia profesional es requerido.")
            .MaximumLength(20).WithMessage("La tarjeta profesional no puede exceder 20 caracteres.")
            .MustAsync(async (license, cancellationToken) =>
                !await unitOfWork.VeterinariansRepository.ExistsByLicenseNumberAsync(license, cancellationToken))
            .WithMessage("Ya existe un veterinario registrado con esa tarjeta profesional.");
    }
}
