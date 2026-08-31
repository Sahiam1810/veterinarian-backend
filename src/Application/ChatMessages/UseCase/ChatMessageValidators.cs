using FluentValidation;

namespace Application.ChatMessages.UseCase;

public sealed class CreateChatMessageCommandValidator
    : AbstractValidator<CreateChatMessageCommand>
{
    public CreateChatMessageCommandValidator()
    {
        RuleFor(command => command.ChatConversationId)
            .NotEmpty()
            .WithMessage("El identificador de la conversación es obligatorio.");

        RuleFor(command => command.ChatParticipantId)
            .NotEmpty()
            .WithMessage("El identificador del participante es obligatorio.");

        RuleFor(command => command.SenderTypesId)
            .NotEmpty()
            .WithMessage("El identificador del tipo de remitente es obligatorio.");

        RuleFor(command => command.MessageTypeId)
            .NotEmpty()
            .WithMessage("El identificador del tipo de mensaje es obligatorio.");

        RuleFor(command => command.Content)
            .NotEmpty()
            .WithMessage("El contenido del mensaje es obligatorio.");
    }
}

public sealed class GetChatMessageByIdQueryValidator
    : AbstractValidator<GetChatMessageByIdQuery>
{
    public GetChatMessageByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty()
            .WithMessage("El identificador del mensaje es obligatorio.");
    }
}

public sealed class GetChatMessagesByConversationIdQueryValidator
    : AbstractValidator<GetChatMessagesByConversationIdQuery>
{
    public GetChatMessagesByConversationIdQueryValidator()
    {
        RuleFor(query => query.ChatConversationId)
            .NotEmpty()
            .WithMessage("El identificador de la conversación es obligatorio.");
    }
}
