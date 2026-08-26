using FluentValidation;

namespace Application.SenderTypes.UseCases;

public sealed class CreateSenderTypeCommandValidator : AbstractValidator<CreateSenderTypeCommand>
{
    public CreateSenderTypeCommandValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
}

public sealed class UpdateSenderTypeCommandValidator : AbstractValidator<UpdateSenderTypeCommand>
{
    public UpdateSenderTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
    }
}
