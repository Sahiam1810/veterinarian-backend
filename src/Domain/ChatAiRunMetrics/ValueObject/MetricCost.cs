namespace Domain.ChatAiRunMetrics.ValueObjects;

// Value object del costo (Oracle NUMBER(10,6)).
public sealed record MetricCost
{
    // Precisión total del NUMBER en Oracle.
    public const int Precision = 10;

    // Escala decimal del NUMBER en Oracle.
    public const int Scale = 6;

    // Máximo permitido con precisión 10 y escala 6.
    public static readonly decimal MaxValue = 9999.999999m;

    // Solo se construye vía Create para garantizar validación.
    private MetricCost(decimal? value)
    {
        Value = value;
    }

    // Costo monetario; null si no se informó.
    public decimal? Value { get; }

    // Crea un MetricCost validando rango y redondeando a 6 decimales.
    public static MetricCost Create(decimal? value)
    {
        // Sin costo informado: se permite null.
        if (value is null)
        {
            return new MetricCost((decimal?)null);
        }

        // El costo no puede ser negativo.
        if (value < 0)
        {
            throw new ArgumentException(
                "El costo no puede ser negativo.",
                nameof(value));
        }

        // No superar el máximo de NUMBER(10,6).
        if (value > MaxValue)
        {
            throw new ArgumentException(
                $"El costo no puede superar {MaxValue}.",
                nameof(value));
        }

        // Alinea el valor a la escala Oracle (6 decimales).
        return new MetricCost(decimal.Round(value.Value, Scale, MidpointRounding.AwayFromZero));
    }

    public override string ToString() => Value?.ToString() ?? string.Empty;
}
