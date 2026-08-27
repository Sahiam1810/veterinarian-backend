using FluentValidation;

namespace Application.AiModels.UseCase;

public sealed class UpdateAiModelCommandValidator : AbstractValidator<UpdateAiModelCommand>
{
    private const int NameMaxLength = 150;
    private const int ModelKeyMaxLength = 150;

    public UpdateAiModelCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.NameModel)
            .NotEmpty()
            .WithMessage("El nombre del modelo es obligatorio.")
            .MaximumLength(NameMaxLength)
            .WithMessage($"El nombre del modelo no puede superar los {NameMaxLength} caracteres.");

        RuleFor(command => command.ModelKey)
            .NotEmpty()
            .WithMessage("La clave del modelo es obligatoria.")
            .MaximumLength(ModelKeyMaxLength)
            .WithMessage($"La clave del modelo no puede superar los {ModelKeyMaxLength} caracteres.");

        RuleFor(command => command.InputTokenPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("El precio por token de entrada no puede ser negativo.");

        RuleFor(command => command.OutputTokenPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("El precio por token de salida no puede ser negativo.");

        RuleFor(command => command.MaxTokens)
            .GreaterThanOrEqualTo(0)
            .WithMessage("El límite de tokens no puede ser negativo.");

        RuleFor(command => command.ContextWindow)
            .GreaterThanOrEqualTo(0)
            .WithMessage("La ventana de contexto no puede ser negativa.");
    }
}
