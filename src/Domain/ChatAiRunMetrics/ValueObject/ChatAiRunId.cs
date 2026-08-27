namespace Domain.ChatAiRunMetrics.ValueObjects;

// Value object del identificador de ejecución de IA (ai_run_id).
public sealed record ChatAiRunId
{
    // Solo se construye vía Create para garantizar validación.
    private ChatAiRunId(Guid value)
    {
        Value = value;
    }

    // Valor Guid subyacente.
    public Guid Value { get; }

    // Crea un ChatAiRunId o lanza si el Guid está vacío.
    public static ChatAiRunId Create(Guid value)
    {
        // Guid.Empty no es un identificador válido de negocio.
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la ejecución de IA es obligatorio.",
                nameof(value));
        }

        return new ChatAiRunId(value);
    }

    public override string ToString() => Value.ToString();
}
