namespace Domain.ProviderModelsAi.ValueObjects;

public sealed record ProviderName
{
    public const int MaxLength = 150;

    private ProviderName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ProviderName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("El nombre del proveedor es obligatorio.", nameof(value));
        }

        var providerName = value.Trim();

        if (providerName.Length > MaxLength)
        {
            throw new ArgumentException(
                $"El nombre del proveedor no puede superar los {MaxLength} caracteres.",
                nameof(value));
        }

        return new ProviderName(providerName);
    }

    public override string ToString() => Value;
}
