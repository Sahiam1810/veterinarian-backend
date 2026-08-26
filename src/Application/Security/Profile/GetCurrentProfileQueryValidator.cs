using FluentValidation;
namespace Application.Security.Profile;

public sealed class GetCurrentProfileQueryValidator
    : AbstractValidator<GetCurrentProfileQuery>
{
    public GetCurrentProfileQueryValidator() =>
        RuleFor(query => query.UserAccountId).NotEmpty();
}