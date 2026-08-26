namespace Domain.ChatAiRunErrors.ValueObjects;

// Value object del código de error (Oracle VARCHAR2(80), columna error_code).
public sealed record ChatAiErrorCode
{
    // Longitud máxima alineada a VARCHAR2(80).
    public const int MaxLength = 80;

    // Solo se construye vía Create para garantizar validación.
    private ChatAiErrorCode(string? value)
    {
        Value = value;
    }

    // Código de error; null si no se informó.
    public string? Value { get; }

    // Crea un ChatAiErrorCode o lanza si supera MaxLength.
    public static ChatAiErrorCode Create(string? value)
    {
        // Sin código: se permite null.
        if (string.IsNullOrWhiteSpace(value))
        {
            return new ChatAiErrorCode((string?)null);
        }

        var errorCode = value.Trim();

        // No superar VARCHAR2(80) de Oracle.
        if (errorCode.Length > MaxLength)
        {
            throw new ArgumentException(
                $"El código de error no puede superar los {MaxLength} caracteres.",
                nameof(value));
        }

        return new ChatAiErrorCode(errorCode);
    }

    public override string ToString() => Value ?? string.Empty;
}
