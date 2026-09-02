namespace Domain.Clients.ValueObjects;

// Teléfono de contacto general del cliente (opcional, distinto del de la cita).
public sealed record ClientPhoneNumber
{
    public const int MaxLength = 20;

    private ClientPhoneNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ClientPhoneNumber? CreateOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Create(value);
    }

    public static ClientPhoneNumber Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "El número de teléfono es obligatorio.",
                nameof(value));
        }

        var normalized = Normalize(value);
        if (normalized.Length < 7 || normalized.Length > MaxLength)
        {
            throw new ArgumentException(
                $"El teléfono debe tener entre 7 y {MaxLength} dígitos.",
                nameof(value));
        }

        return new ClientPhoneNumber(normalized);
    }

    // Conserva solo dígitos para comparar y persistir de forma estable.
    public static string Normalize(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits;
    }

    public override string ToString() => Value;
}
