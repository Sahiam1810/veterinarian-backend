using FluentValidation;

namespace Application.Specialties.UseCases;

public sealed class CreateSpecialtyCommandValidator : AbstractValidator<CreateSpecialtyCommand>
{
    public CreateSpecialtyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Description).MaximumLength(30);
    }
}
public sealed class UpdateSpecialtyCommandValidator : AbstractValidator<UpdateSpecialtyCommand>
{
    public UpdateSpecialtyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Description).MaximumLength(30);
    }
}
