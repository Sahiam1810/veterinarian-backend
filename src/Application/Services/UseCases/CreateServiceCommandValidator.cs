using Application.Common.Abstractions;
using FluentValidation;

namespace Application.Services.UseCases;

public sealed class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
{
    public CreateServiceCommandValidator(IUnitOfWork unitOfWork)
    {
        RuleFor(x => x.TypeServiceId)
            .NotEmpty().WithMessage("El tipo de servicio es requerido.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(50).WithMessage("El nombre no puede exceder 50 caracteres.")
            .MustAsync(async (name, cancellationToken) =>
                !await unitOfWork.ServicesRepository.ExistsByNameAsync(name, cancellationToken))
            .WithMessage("Ya existe un servicio con el mismo nombre.");

        RuleFor(x => x.DurationMinutes)
            .GreaterThan(0).WithMessage("La duración debe ser mayor a 0 minutos.");

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("El precio no puede ser negativo.");
    }
}
