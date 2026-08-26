namespace Domain.ChatAiRunErrors.ValueObjects;

// Value object del mensaje de error (Oracle CLOB, columna error_message).
public sealed record ChatAiErrorMessage
{
    // Solo se construye vía Create para garantizar validación.
    private ChatAiErrorMessage(string? value)
    {
        Value = value;
    }

    // Texto del error; null si no se informó.
    public string? Value { get; }

    // Crea un ChatAiErrorMessage; vacío o blanco se normaliza a null (CLOB sin tope en domain).
    public static ChatAiErrorMessage Create(string? value)
    {
        // Sin mensaje: se permite null.
        if (string.IsNullOrWhiteSpace(value))
        {
            return new ChatAiErrorMessage((string?)null);
        }

        return new ChatAiErrorMessage(value.Trim());
    }

    public override string ToString() => Value ?? string.Empty;
}
