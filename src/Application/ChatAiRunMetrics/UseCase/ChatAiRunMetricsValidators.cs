using FluentValidation;

namespace Application.ChatAiRunMetrics.UseCase;

public sealed class CreateChatAiRunMetricsCommandValidator
    : AbstractValidator<CreateChatAiRunMetricsCommand>
{
    public CreateChatAiRunMetricsCommandValidator()
    {
        RuleFor(command => command.ChatAiRunId)
            .NotEmpty()
            .WithMessage("El identificador de la ejecución de IA es obligatorio.");

        RuleFor(command => command.PromptTokens)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Los tokens de prompt no pueden ser negativos.");

        RuleFor(command => command.CompletionTokens)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Los tokens de completado no pueden ser negativos.");

        RuleFor(command => command.TotalTokens)
            .GreaterThanOrEqualTo(0)
            .WithMessage("El total de tokens no puede ser negativo.");

        RuleFor(command => command.Cost)
            .GreaterThanOrEqualTo(0)
            .WithMessage("El costo no puede ser negativo.");

        RuleFor(command => command)
            .Must(command => command.TotalTokens == command.PromptTokens + command.CompletionTokens)
            .WithMessage("El total de tokens debe ser igual a la suma de tokens de prompt y completado.");
    }
}

public sealed class GetChatAiRunMetricsByIdQueryValidator
    : AbstractValidator<GetChatAiRunMetricsByIdQuery>
{
    public GetChatAiRunMetricsByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty()
            .WithMessage("El identificador de las métricas es obligatorio.");
    }
}

public sealed class GetChatAiRunMetricsByChatAiRunIdQueryValidator
    : AbstractValidator<GetChatAiRunMetricsByChatAiRunIdQuery>
{
    public GetChatAiRunMetricsByChatAiRunIdQueryValidator()
    {
        RuleFor(query => query.ChatAiRunId)
            .NotEmpty()
            .WithMessage("El identificador de la ejecución de IA es obligatorio.");
    }
}
