using Application.Common.Abstractions;
using FluentValidation;

namespace Application.Veterinarians.UseCases;

public sealed class UpdateVeterinarianCommandValidator : AbstractValidator<UpdateVeterinarianCommand>
{
    public UpdateVeterinarianCommandValidator(IUnitOfWork unitOfWork)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El id del veterinario es requerido.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("El usuario es requerido.");

        RuleFor(x => x.SpecialtyId)
            .NotEmpty().WithMessage("La especialidad es requerida.");

        RuleFor(x => x.LicenseNumber)
            .NotEmpty().WithMessage("El número de tarjeta/licencia profesional es requerido.")
            .MaximumLength(20).WithMessage("La tarjeta profesional no puede exceder 20 caracteres.")
            .MustAsync(async (command, license, cancellationToken) =>
                !await unitOfWork.VeterinariansRepository.ExistsByLicenseNumberAsync(license, cancellationToken, command.Id))
            .WithMessage("Ya existe otro veterinario con la misma tarjeta profesional.");
    }
}
