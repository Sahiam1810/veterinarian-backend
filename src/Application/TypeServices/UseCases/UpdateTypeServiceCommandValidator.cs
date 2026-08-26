using Application.Common.Abstractions;
using FluentValidation;

namespace Application.TypeServices.UseCases;

public sealed class UpdateTypeServiceCommandValidator : AbstractValidator<UpdateTypeServiceCommand>
{
    public UpdateTypeServiceCommandValidator(IUnitOfWork unitOfWork)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El id es requerido.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(50).WithMessage("El nombre no puede exceder 50 caracteres.")
            .MustAsync(async (command, name, cancellationToken) =>
                !await unitOfWork.TypeServicesRepository.ExistsByNameAsync(name, cancellationToken, command.Id))
            .WithMessage("Ya existe otro tipo de servicio con el mismo nombre.");

        RuleFor(x => x.Description)
            .MaximumLength(200).WithMessage("La descripción no puede exceder 200 caracteres.");
    }
}
