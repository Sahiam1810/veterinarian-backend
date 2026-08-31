
using Domain.Modules.ValueObjects;


using FluentValidation;

namespace Application.Modules.UseCases;



// Valida creación de módulo.

public sealed class CreateModuleCommandValidator : AbstractValidator<CreateModuleCommand>
{
    public CreateModuleCommandValidator()
    {

        RuleFor(command => command.Name)
            .NotEmpty()
            .WithMessage("El nombre del módulo es obligatorio.")
            .MaximumLength(ModuleName.MaxLength)
            .WithMessage(
                $"El nombre del módulo no puede superar los {ModuleName.MaxLength} caracteres.");
    }
}

        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);
    }
}

// Valida actualización de módulo.

public sealed class UpdateModuleCommandValidator : AbstractValidator<UpdateModuleCommand>
{
    public UpdateModuleCommandValidator()
    {

        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Name)
            .NotEmpty()
            .WithMessage("El nombre del módulo es obligatorio.")
            .MaximumLength(ModuleName.MaxLength)
            .WithMessage(
                $"El nombre del módulo no puede superar los {ModuleName.MaxLength} caracteres.");
    }
}

public sealed class DeleteModuleCommandValidator : AbstractValidator<DeleteModuleCommand>
{
    public DeleteModuleCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();
    }
}

public sealed class GetModuleByIdQueryValidator : AbstractValidator<GetModuleByIdQuery>
{
    public GetModuleByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty();

        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description is not null);

    }
}
