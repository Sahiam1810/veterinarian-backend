using FluentValidation;

namespace Application.ChatEscalationResolutions.UseCase;

public sealed class CreateChatEscalationResolutionCommandValidator
    : AbstractValidator<CreateChatEscalationResolutionCommand>
{
    public CreateChatEscalationResolutionCommandValidator()
    {
        RuleFor(command => command.ChatEscalationId)
            .NotEmpty()
            .WithMessage("El identificador del escalamiento es obligatorio.");

        RuleFor(command => command.ResolvedBy)
            .Must(resolvedBy => !resolvedBy.HasValue || resolvedBy.Value != Guid.Empty)
            .WithMessage("El identificador de quien resuelve no puede ser vacío.");
    }
}

public sealed class GetChatEscalationResolutionByIdQueryValidator
    : AbstractValidator<GetChatEscalationResolutionByIdQuery>
{
    public GetChatEscalationResolutionByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty()
            .WithMessage("El identificador de la resolución es obligatorio.");
    }
}

public sealed class GetChatEscalationResolutionsByChatEscalationIdQueryValidator
    : AbstractValidator<GetChatEscalationResolutionsByChatEscalationIdQuery>
{
    public GetChatEscalationResolutionsByChatEscalationIdQueryValidator()
    {
        RuleFor(query => query.ChatEscalationId)
            .NotEmpty()
            .WithMessage("El identificador del escalamiento es obligatorio.");
    }
}

public sealed class UpdateChatEscalationResolutionCommandValidator
    : AbstractValidator<UpdateChatEscalationResolutionCommand>
{
    public UpdateChatEscalationResolutionCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("El identificador de la resolución es obligatorio.");

        RuleFor(command => command.ResolvedBy)
            .Must(resolvedBy => !resolvedBy.HasValue || resolvedBy.Value != Guid.Empty)
            .WithMessage("El identificador de quien resuelve no puede ser vacío.");
    }
}

public sealed class DeleteChatEscalationResolutionCommandValidator
    : AbstractValidator<DeleteChatEscalationResolutionCommand>
{
    public DeleteChatEscalationResolutionCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("El identificador de la resolución es obligatorio.");
    }
}
