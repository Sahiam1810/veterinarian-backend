using FluentValidation;

namespace Application.Telegram.Updates;

public sealed class IngestTelegramUpdateCommandValidator
    : AbstractValidator<IngestTelegramUpdateCommand>
{
    public IngestTelegramUpdateCommandValidator()
    {
        RuleFor(command => command.UpdateId).GreaterThan(0);
        RuleFor(command => command.TelegramUserId).GreaterThan(0);
        RuleFor(command => command.TelegramChatId).GreaterThan(0);
        RuleFor(command => command.TelegramMessageId).GreaterThan(0);
        RuleFor(command => command.ChatType).NotEmpty().MaximumLength(30);
        RuleFor(command => command.Text).MaximumLength(4096);
    }
}
