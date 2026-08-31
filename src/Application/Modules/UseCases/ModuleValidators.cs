using Domain.Modules.ValueObjects;
using FluentValidation;

namespace Application.Modules.UseCases;

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

        RuleFor(command => command.Description)
            .MaximumLength(1000)
            .WithMessage("La descripción no puede superar los 1000 caracteres.")
            .When(command => command.Description is not null);
    }
}

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

        RuleFor(command => command.Description)
            .MaximumLength(1000)
            .WithMessage("La descripción no puede superar los 1000 caracteres.")
            .When(command => command.Description is not null);
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
    }
}
