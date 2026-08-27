using FluentValidation;

namespace Application.MessageTypes.UseCases;

public sealed class CreateMessageTypeCommandValidator : AbstractValidator<CreateMessageTypeCommand>
{
    public CreateMessageTypeCommandValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
}

public sealed class UpdateMessageTypeCommandValidator : AbstractValidator<UpdateMessageTypeCommand>
{
    public UpdateMessageTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
    }
}
