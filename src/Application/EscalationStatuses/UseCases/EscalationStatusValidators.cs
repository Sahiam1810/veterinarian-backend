using FluentValidation;

namespace Application.EscalationStatuses.UseCases;

// Valida creación de estado de escalamiento.
public sealed class CreateEscalationStatusCommandValidator : AbstractValidator<CreateEscalationStatusCommand>
{
    public CreateEscalationStatusCommandValidator() =>
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
}

// Valida actualización de estado de escalamiento.
public sealed class UpdateEscalationStatusCommandValidator : AbstractValidator<UpdateEscalationStatusCommand>
{
    public UpdateEscalationStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
    }
}
