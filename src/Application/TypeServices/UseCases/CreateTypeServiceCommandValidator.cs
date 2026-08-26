using Application.Common.Abstractions;
using FluentValidation;

namespace Application.TypeServices.UseCases;

public sealed class CreateTypeServiceCommandValidator : AbstractValidator<CreateTypeServiceCommand>
{
    public CreateTypeServiceCommandValidator(IUnitOfWork unitOfWork)
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(50).WithMessage("El nombre no puede exceder 50 caracteres.")
            .MustAsync(async (name, cancellationToken) =>
                !await unitOfWork.TypeServicesRepository.ExistsByNameAsync(name, cancellationToken))
            .WithMessage("Ya existe un tipo de servicio con el mismo nombre.");

        RuleFor(x => x.Description)
            .MaximumLength(200).WithMessage("La descripción no puede exceder 200 caracteres.");
    }
}
