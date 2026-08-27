using FluentValidation;

namespace Application.Priorities.UseCases;

// Valida creación de prioridad.
public sealed class CreatePriorityCommandValidator : AbstractValidator<CreatePriorityCommand>
{
    public CreatePriorityCommandValidator() =>
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
}

// Valida actualización de prioridad.
public sealed class UpdatePriorityCommandValidator : AbstractValidator<UpdatePriorityCommand>
{
    public UpdatePriorityCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
    }
}
