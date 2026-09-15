using Domain.Users.ValueObjects;
using FluentValidation;

namespace Application.Security.Profile;

public sealed class UpdateMyPhotoCommandValidator
    : AbstractValidator<UpdateMyPhotoCommand>
{
    public UpdateMyPhotoCommandValidator()
    {
        RuleFor(command => command.UserAccountId)
            .NotEmpty();

        RuleFor(command => command.PhotoUrl)
            .MaximumLength(UserPhotoUrl.MaxLength)
            .WithMessage($"La URL de la foto no puede superar los {UserPhotoUrl.MaxLength} caracteres.")
            .Must(url =>
                string.IsNullOrWhiteSpace(url)
                || (Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            .WithMessage("La URL de la foto debe ser una dirección http o https válida.");
    }
}
