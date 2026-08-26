using FluentValidation;

namespace Application.AiRunStatuses.UseCases;

public sealed class CreateAiRunStatusCommandValidator : AbstractValidator<CreateAiRunStatusCommand>
{
    public CreateAiRunStatusCommandValidator() => RuleFor(x => x.NameStatus).NotEmpty().MaximumLength(50);
}

public sealed class UpdateAiRunStatusCommandValidator : AbstractValidator<UpdateAiRunStatusCommand>
{
    public UpdateAiRunStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NameStatus).NotEmpty().MaximumLength(50);
    }
}
