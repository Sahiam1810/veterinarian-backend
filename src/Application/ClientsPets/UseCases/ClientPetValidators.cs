using FluentValidation;

namespace Application.ClientsPets.UseCases;

public sealed class CreateClientPetCommandValidator : AbstractValidator<CreateClientPetCommand>
{
    public CreateClientPetCommandValidator()
    {
        RuleFor(x => x.ClientId).NotEmpty();
        RuleFor(x => x.PetId).NotEmpty();
    }
}
public sealed class UpdateClientPetCommandValidator : AbstractValidator<UpdateClientPetCommand>
{
    public UpdateClientPetCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}
