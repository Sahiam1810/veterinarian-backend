using FluentValidation;

namespace Application.Agent.Messages;

public sealed class SendAgentMessageCommandValidator : AbstractValidator<SendAgentMessageCommand>
{
    public SendAgentMessageCommandValidator()
    {
        RuleFor(command => command.Message).NotEmpty().MaximumLength(8000);
        RuleFor(command => command.Language).NotEmpty().MaximumLength(20);
        RuleFor(command => command.PersonId).NotEmpty();
        RuleFor(command => command.Role).NotEmpty().MaximumLength(80);
        RuleFor(command => command.IdempotencyKey).NotEmpty().MaximumLength(160);
        RuleFor(command => command.CorrelationId).NotEmpty();
    }
}
