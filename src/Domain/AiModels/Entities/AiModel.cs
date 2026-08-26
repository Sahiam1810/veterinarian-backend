using Domain.AiModels.ValueObjects;
using Domain.Common;

namespace Domain.AiModels.Entities;

public sealed class AiModel : BaseEntity<Guid>
{
    private AiModel()
    {
    }

    public Guid ProviderModelAiId { get; private set; }

    public string NameModel { get; private set; } = string.Empty;

    public string ModelKey { get; private set; } = string.Empty;

    public decimal InputTokenPrice { get; private set; }

    public decimal OutputTokenPrice { get; private set; }

    public int MaxTokens { get; private set; }

    public int ContextWindow { get; private set; }

    public bool IsActive { get; private set; }

    public static AiModel Create(
        Guid providerModelAiId,
        string nameModel,
        string modelKey,
        decimal inputTokenPrice,
        decimal outputTokenPrice,
        int maxTokens,
        int contextWindow)
    {
        ValidateProvider(providerModelAiId);
        ValidateRequiredText(nameModel, nameof(nameModel), "El nombre del modelo es obligatorio.");
        ValidateRequiredText(modelKey, nameof(modelKey), "La clave del modelo es obligatoria.");

        var model = new AiModel
        {
            Id = Guid.NewGuid(),
            ProviderModelAiId = providerModelAiId,
            NameModel = nameModel.Trim(),
            ModelKey = modelKey.Trim(),
            InputTokenPrice = TokenPrice.Create(inputTokenPrice).Value,
            OutputTokenPrice = TokenPrice.Create(outputTokenPrice).Value,
            MaxTokens = TokenLimit.Create(maxTokens).Value,
            ContextWindow = TokenLimit.Create(contextWindow).Value,
            IsActive = true
        };

        return model;
    }

    public void Update(
        string nameModel,
        string modelKey,
        decimal inputTokenPrice,
        decimal outputTokenPrice,
        int maxTokens,
        int contextWindow)
    {
        ValidateRequiredText(nameModel, nameof(nameModel), "El nombre del modelo es obligatorio.");
        ValidateRequiredText(modelKey, nameof(modelKey), "La clave del modelo es obligatoria.");

        NameModel = nameModel.Trim();
        ModelKey = modelKey.Trim();
        InputTokenPrice = TokenPrice.Create(inputTokenPrice).Value;
        OutputTokenPrice = TokenPrice.Create(outputTokenPrice).Value;
        MaxTokens = TokenLimit.Create(maxTokens).Value;
        ContextWindow = TokenLimit.Create(contextWindow).Value;
        Touch();
    }

    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateProvider(Guid providerModelAiId)
    {
        if (providerModelAiId == Guid.Empty)
        {
            throw new ArgumentException("El identificador del proveedor es obligatorio.", nameof(providerModelAiId));
        }
    }

    private static void ValidateRequiredText(string value, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message, paramName);
        }
    }
}
