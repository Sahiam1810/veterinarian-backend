using Domain.Common;
using Domain.ProviderModelsAi.ValueObjects;

namespace Domain.ProviderModelsAi.Entities;

public sealed class ProviderModelAi : BaseEntity<Guid>
{
    private ProviderModelAi()
    {
    }

    public string NameProviderAi { get; private set; } = string.Empty;

    public string? BusinessName { get; private set; }

    public string? Website { get; private set; }

    public bool IsActive { get; private set; }

    public static ProviderModelAi Create(
        string nameProviderAi,
        string? businessName,
        string? website)
    {
        var provider = new ProviderModelAi
        {
            Id = Guid.NewGuid(),
            NameProviderAi = ProviderName.Create(nameProviderAi).Value,
            BusinessName = NormalizeOptional(businessName),
            Website = NormalizeOptional(website),
            IsActive = true
        };

        return provider;
    }

    public void Update(string nameProviderAi, string? businessName, string? website)
    {
        NameProviderAi = ProviderName.Create(nameProviderAi).Value;
        BusinessName = NormalizeOptional(businessName);
        Website = NormalizeOptional(website);
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

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
