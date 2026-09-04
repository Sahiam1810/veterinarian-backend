using System.Diagnostics.CodeAnalysis;

namespace Domain.Clients.ValueObjects;

// Teléfono de contacto general del cliente (opcional en persistencia histórica,
// distinto del de la cita). Create/Update de API lo exigen vía validador.
public sealed record ClientPhoneNumber
{
    public const int MinLength = 7;
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

    public static ClientPhoneNumber Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "El número de teléfono es obligatorio.",
                nameof(value));
        }

        if (!TryCreate(value, out var phone))
        {
            throw new ArgumentException(
                $"El teléfono debe tener entre {MinLength} y {MaxLength} dígitos.",
                nameof(value));
        }

        return phone;
    }

    public static bool TryCreate(string? value, [NotNullWhen(true)] out ClientPhoneNumber? phone)
    {
        phone = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = Normalize(value);
        if (normalized.Length < MinLength || normalized.Length > MaxLength)
        {
            return false;
        }

        phone = new ClientPhoneNumber(normalized);
        return true;
    }

    // Conserva solo dígitos para comparar y persistir de forma estable.
    public static string Normalize(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits;
    }

    public override string ToString() => Value;
}
