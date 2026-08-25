namespace Domain.ChatAiRunMetrics.ValueObjects;

// Value object de cantidad de tokens (nullable, no negativa).
public sealed record TokenCount
{
    // Solo se construye vía Create para garantizar validación.
    private TokenCount(int? value)
    {
        Value = value;
    }

    // Cantidad de tokens; null si no se informó.
    public int? Value { get; }

    // Crea un TokenCount o lanza si el valor es negativo.
    public static TokenCount Create(int? value)
    {
        // Los tokens no pueden ser negativos.
        if (value is < 0)
        {
            throw new ArgumentException(
                "La cantidad de tokens no puede ser negativa.",
                nameof(value));
        }

        return new TokenCount(value);
    }

    public override string ToString() => Value?.ToString() ?? string.Empty;
}
