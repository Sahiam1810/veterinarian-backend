namespace Domain.Appointments.ValueObjects;

public sealed record BookingRequestKeyHash
{
    public const int Length = 64;

    private BookingRequestKeyHash(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static BookingRequestKeyHash Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length != Length
            || value.Any(character => !Uri.IsHexDigit(character)))
        {
            throw new ArgumentException(
                $"El hash de idempotencia debe tener {Length} caracteres hexadecimales.",
                nameof(value));
        }

        return new BookingRequestKeyHash(value.ToUpperInvariant());
    }

    public override string ToString() => Value;
}
