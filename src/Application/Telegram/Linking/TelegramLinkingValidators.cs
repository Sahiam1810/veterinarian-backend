using FluentValidation;

namespace Application.Telegram.Linking;

public sealed class CreateTelegramLinkCodeCommandValidator
    : AbstractValidator<CreateTelegramLinkCodeCommand>
{
    public CreateTelegramLinkCodeCommandValidator() =>
        RuleFor(command => command.PersonId).NotEmpty();
}

public sealed class ConsumeTelegramLinkCodeCommandValidator
    : AbstractValidator<ConsumeTelegramLinkCodeCommand>
{
    public ConsumeTelegramLinkCodeCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(128);
        RuleFor(command => command.TelegramUserId).GreaterThan(0);
        RuleFor(command => command.TelegramChatId).GreaterThan(0);
    }
}
