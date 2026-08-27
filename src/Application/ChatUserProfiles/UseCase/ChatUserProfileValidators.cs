using FluentValidation;

namespace Application.ChatUserProfiles.UseCase;

public sealed class CreateChatUserProfileCommandValidator
    : AbstractValidator<CreateChatUserProfileCommand>
{
    public CreateChatUserProfileCommandValidator()
    {
        RuleFor(command => command.PersonId)
            .NotEmpty()
            .WithMessage("El identificador de la persona es obligatorio.");
    }
}

public sealed class GetChatUserProfileByIdQueryValidator
    : AbstractValidator<GetChatUserProfileByIdQuery>
{
    public GetChatUserProfileByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .NotEmpty()
            .WithMessage("El identificador del perfil es obligatorio.");
    }
}

public sealed class GetChatUserProfilesByPersonIdQueryValidator
    : AbstractValidator<GetChatUserProfilesByPersonIdQuery>
{
    public GetChatUserProfilesByPersonIdQueryValidator()
    {
        RuleFor(query => query.PersonId)
            .NotEmpty()
            .WithMessage("El identificador de la persona es obligatorio.");
    }
}

public sealed class UpdateChatUserProfileCommandValidator
    : AbstractValidator<UpdateChatUserProfileCommand>
{
    public UpdateChatUserProfileCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("El identificador del perfil es obligatorio.");
    }
}

public sealed class DeleteChatUserProfileCommandValidator
    : AbstractValidator<DeleteChatUserProfileCommand>
{
    public DeleteChatUserProfileCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty()
            .WithMessage("El identificador del perfil es obligatorio.");
    }
}
