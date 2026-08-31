using FluentValidation;

namespace Application.ChatEscalationStatusHistories.UseCase;

public sealed class CreateChatEscalationStatusHistoryCommandValidator
    : AbstractValidator<CreateChatEscalationStatusHistoryCommand>
{
    public CreateChatEscalationStatusHistoryCommandValidator()
    {
        RuleFor(command => command.EscalationStatusId)
            .NotEmpty()
            .WithMessage("El identificador del estado de escalamiento es obligatorio.");

        RuleFor(command => command.ChatEscalationId)
            .NotEmpty()
            .WithMessage("El identificador del escalamiento es obligatorio.");
    }
}

public sealed class GetChatEscalationStatusHistoryByIdQueryValidator
    : AbstractValidator<GetChatEscalationStatusHistoryByIdQuery>
{
    public GetChatEscalationStatusHistoryByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty()
            .WithMessage("El identificador del historial es obligatorio.");
    }
}

public sealed class GetChatEscalationStatusHistoriesByChatEscalationIdQueryValidator
    : AbstractValidator<GetChatEscalationStatusHistoriesByChatEscalationIdQuery>
{
    public GetChatEscalationStatusHistoriesByChatEscalationIdQueryValidator()
    {
        RuleFor(query => query.ChatEscalationId)
            .NotEmpty()
            .WithMessage("El identificador del escalamiento es obligatorio.");
    }
}

public sealed class UpdateChatEscalationStatusHistoryCommandValidator
    : AbstractValidator<UpdateChatEscalationStatusHistoryCommand>
{
    public UpdateChatEscalationStatusHistoryCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("El identificador del historial es obligatorio.");

        RuleFor(command => command.EscalationStatusId)
            .NotEmpty()
            .WithMessage("El identificador del estado de escalamiento es obligatorio.");
    }
}

public sealed class DeleteChatEscalationStatusHistoryCommandValidator
    : AbstractValidator<DeleteChatEscalationStatusHistoryCommand>
{
    public DeleteChatEscalationStatusHistoryCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("El identificador del historial es obligatorio.");
    }
}
