using FluentValidation;

namespace Application.ChatAiRunErrors.UseCase;

public sealed class CreateChatAiRunErrorCommandValidator
    : AbstractValidator<CreateChatAiRunErrorCommand>
{
    public CreateChatAiRunErrorCommandValidator()
    {
        RuleFor(command => command.ChatAiRunId)
            .NotEmpty()
            .WithMessage("El identificador de la ejecución de IA es obligatorio.");

        RuleFor(command => command.ErrorMessage)
            .NotEmpty()
            .WithMessage("El mensaje de error es obligatorio.");
    }
}

public sealed class GetChatAiRunErrorByIdQueryValidator
    : AbstractValidator<GetChatAiRunErrorByIdQuery>
{
    public GetChatAiRunErrorByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty()
            .WithMessage("El identificador del error es obligatorio.");
    }
}

public sealed class GetChatAiRunErrorsByChatAiRunIdQueryValidator
    : AbstractValidator<GetChatAiRunErrorsByChatAiRunIdQuery>
{
    public GetChatAiRunErrorsByChatAiRunIdQueryValidator()
    {
        RuleFor(query => query.ChatAiRunId)
            .NotEmpty()
            .WithMessage("El identificador de la ejecución de IA es obligatorio.");
    }
}
