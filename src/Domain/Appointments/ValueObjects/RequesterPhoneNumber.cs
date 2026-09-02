namespace Domain.Appointments.ValueObjects;

// Teléfono con el que se creó esa cita puntual (rastro de origen).
public sealed record RequesterPhoneNumber
{
    public const int MaxLength = 20;

    private RequesterPhoneNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static RequesterPhoneNumber Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "El teléfono del solicitante es obligatorio.",
                nameof(value));
        }

        var normalized = Normalize(value);
        if (normalized.Length < 7 || normalized.Length > MaxLength)
        {
            throw new ArgumentException(
                $"El teléfono del solicitante debe tener entre 7 y {MaxLength} dígitos.",
                nameof(value));
        }

        return new RequesterPhoneNumber(normalized);
    }

    public static string Normalize(string value) =>
        new(value.Where(char.IsDigit).ToArray());

    public bool Matches(string? incoming)
    {
        if (string.IsNullOrWhiteSpace(incoming))
        {
            return false;
        }

        return string.Equals(Value, Normalize(incoming), StringComparison.Ordinal);
    }

    public override string ToString() => Value;
}
