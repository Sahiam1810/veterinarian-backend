using Domain.ProviderModelsAi.ValueObjects;
using FluentValidation;

namespace Application.ProviderModelsAi.UseCase;

public sealed class CreateProviderModelAiCommandValidator : AbstractValidator<CreateProviderModelAiCommand>
{
    private const int BusinessNameMaxLength = 200;
    private const int WebsiteMaxLength = 500;

    public CreateProviderModelAiCommandValidator()
    {
        RuleFor(command => command.NameProviderAi)
            .NotEmpty()
            .WithMessage("El nombre del proveedor es obligatorio.")
            .MaximumLength(ProviderName.MaxLength)
            .WithMessage(
                $"El nombre del proveedor no puede superar los {ProviderName.MaxLength} caracteres.");

        RuleFor(command => command.BusinessName)
            .MaximumLength(BusinessNameMaxLength)
            .WithMessage(
                $"La razón social no puede superar los {BusinessNameMaxLength} caracteres.");

        RuleFor(command => command.Website)
            .MaximumLength(WebsiteMaxLength)
            .WithMessage($"El sitio web no puede superar los {WebsiteMaxLength} caracteres.");
    }
}
