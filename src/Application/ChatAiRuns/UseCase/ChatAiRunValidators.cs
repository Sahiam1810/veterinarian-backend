using FluentValidation;

namespace Application.ChatAiRuns.UseCase;

public sealed class CreateChatAiRunCommandValidator
    : AbstractValidator<CreateChatAiRunCommand>
{
    public CreateChatAiRunCommandValidator()
    {
        RuleFor(command => command.ChatConversationId)
            .NotEmpty()
            .WithMessage("El identificador de la conversación es obligatorio.");

        RuleFor(command => command.ChatMessageId)
            .NotEmpty()
            .WithMessage("El identificador del mensaje es obligatorio.");

        RuleFor(command => command.AiModelId)
            .NotEmpty()
            .WithMessage("El identificador del modelo de IA es obligatorio.");

        RuleFor(command => command.AiRunStatusId)
            .NotEmpty()
            .WithMessage("El identificador del estado de ejecución es obligatorio.");
    }
}

public sealed class UpdateChatAiRunStatusCommandValidator
    : AbstractValidator<UpdateChatAiRunStatusCommand>
{
    public UpdateChatAiRunStatusCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("El identificador de la ejecución de IA es obligatorio.");

        RuleFor(command => command.AiRunStatusId)
            .NotEmpty()
            .WithMessage("El identificador del estado de ejecución es obligatorio.");
    }
}

public sealed class GetChatAiRunByIdQueryValidator
    : AbstractValidator<GetChatAiRunByIdQuery>
{
    public GetChatAiRunByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty()
            .WithMessage("El identificador de la ejecución de IA es obligatorio.");
    }
}

public sealed class GetChatAiRunsByConversationIdQueryValidator
    : AbstractValidator<GetChatAiRunsByConversationIdQuery>
{
    public GetChatAiRunsByConversationIdQueryValidator()
    {
        RuleFor(query => query.ChatConversationId)
            .NotEmpty()
            .WithMessage("El identificador de la conversación es obligatorio.");
    }
}
