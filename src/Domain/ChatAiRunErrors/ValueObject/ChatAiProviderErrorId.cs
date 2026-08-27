namespace Domain.ChatAiRunErrors.ValueObjects;

// Value object del id de error del proveedor (Oracle VARCHAR2(120), columna provider_error_id).
public sealed record ChatAiProviderErrorId
{
    // Longitud máxima alineada a VARCHAR2(120).
    public const int MaxLength = 120;

    // Solo se construye vía Create para garantizar validación.
    private ChatAiProviderErrorId(string? value)
    {
        Value = value;
    }

    // Identificador del error en el proveedor; null si no se informó.
    public string? Value { get; }

    // Crea un ChatAiProviderErrorId o lanza si supera MaxLength.
    public static ChatAiProviderErrorId Create(string? value)
    {
        // Sin id de proveedor: se permite null.
        if (string.IsNullOrWhiteSpace(value))
        {
            return new ChatAiProviderErrorId((string?)null);
        }

        var providerErrorId = value.Trim();

        // No superar VARCHAR2(120) de Oracle.
        if (providerErrorId.Length > MaxLength)
        {
            throw new ArgumentException(
                $"El identificador de error del proveedor no puede superar los {MaxLength} caracteres.",
                nameof(value));
        }

        return new ChatAiProviderErrorId(providerErrorId);
    }

    public override string ToString() => Value ?? string.Empty;
}
