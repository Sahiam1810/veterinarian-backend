using FluentValidation;

namespace Application.ChatConversationAiSettings.UseCase;

public sealed class CreateChatConversationAiSettingCommandValidator
    : AbstractValidator<CreateChatConversationAiSettingCommand>
{
    public CreateChatConversationAiSettingCommandValidator()
    {
        RuleFor(command => command.ConversationId)
            .NotEmpty()
            .WithMessage("El identificador de la conversación es obligatorio.");
    }
}

public sealed class GetChatConversationAiSettingByIdQueryValidator
    : AbstractValidator<GetChatConversationAiSettingByIdQuery>
{
    public GetChatConversationAiSettingByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty()
            .WithMessage("El identificador de la configuración es obligatorio.");
    }
}

public sealed class GetChatConversationAiSettingByConversationIdQueryValidator
    : AbstractValidator<GetChatConversationAiSettingByConversationIdQuery>
{
    public GetChatConversationAiSettingByConversationIdQueryValidator()
    {
        RuleFor(query => query.ConversationId)
            .NotEmpty()
            .WithMessage("El identificador de la conversación es obligatorio.");
    }
}

public sealed class UpdateChatConversationAiSettingCommandValidator
    : AbstractValidator<UpdateChatConversationAiSettingCommand>
{
    public UpdateChatConversationAiSettingCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("El identificador de la configuración es obligatorio.");
    }
}

public sealed class DeleteChatConversationAiSettingCommandValidator
    : AbstractValidator<DeleteChatConversationAiSettingCommand>
{
    public DeleteChatConversationAiSettingCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("El identificador de la configuración es obligatorio.");
    }
}
