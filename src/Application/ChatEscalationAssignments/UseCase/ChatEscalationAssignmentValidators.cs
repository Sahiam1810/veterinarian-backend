using FluentValidation;

namespace Application.ChatEscalationAssignments.UseCase;

public sealed class CreateChatEscalationAssignmentCommandValidator
    : AbstractValidator<CreateChatEscalationAssignmentCommand>
{
    public CreateChatEscalationAssignmentCommandValidator()
    {
        RuleFor(command => command.AgentHumanId)
            .NotEmpty()
            .WithMessage("El identificador del agente humano es obligatorio.");

        RuleFor(command => command.ChatEscalationId)
            .NotEmpty()
            .WithMessage("El identificador del escalamiento es obligatorio.");
    }
}

public sealed class GetChatEscalationAssignmentByIdQueryValidator
    : AbstractValidator<GetChatEscalationAssignmentByIdQuery>
{
    public GetChatEscalationAssignmentByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty()
            .WithMessage("El identificador de la asignación es obligatorio.");
    }
}

public sealed class GetChatEscalationAssignmentsByChatEscalationIdQueryValidator
    : AbstractValidator<GetChatEscalationAssignmentsByChatEscalationIdQuery>
{
    public GetChatEscalationAssignmentsByChatEscalationIdQueryValidator()
    {
        RuleFor(query => query.ChatEscalationId)
            .NotEmpty()
            .WithMessage("El identificador del escalamiento es obligatorio.");
    }
}

public sealed class GetChatEscalationAssignmentsByAgentHumanIdQueryValidator
    : AbstractValidator<GetChatEscalationAssignmentsByAgentHumanIdQuery>
{
    public GetChatEscalationAssignmentsByAgentHumanIdQueryValidator()
    {
        RuleFor(query => query.AgentHumanId)
            .NotEmpty()
            .WithMessage("El identificador del agente humano es obligatorio.");
    }
}

public sealed class UpdateChatEscalationAssignmentCommandValidator
    : AbstractValidator<UpdateChatEscalationAssignmentCommand>
{
    public UpdateChatEscalationAssignmentCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("El identificador de la asignación es obligatorio.");

        RuleFor(command => command.AgentHumanId)
            .NotEmpty()
            .WithMessage("El identificador del agente humano es obligatorio.");
    }
}

public sealed class DeleteChatEscalationAssignmentCommandValidator
    : AbstractValidator<DeleteChatEscalationAssignmentCommand>
{
    public DeleteChatEscalationAssignmentCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("El identificador de la asignación es obligatorio.");
    }
}
