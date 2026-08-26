namespace Domain.Clients.ValueObjects;

public sealed record ClientAddress
{
    public const int MaxLength = 20;

    private ClientAddress(string? value)
    {
        Value = value;
    }

    public string? Value { get; }

    public static ClientAddress Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new ClientAddress((string?)null);
        }

        var address = value.Trim();

        if (address.Length > MaxLength)
        {
            throw new ArgumentException(
                $"La dirección no puede superar los {MaxLength} caracteres.",
                nameof(value));
        }

        return new ClientAddress(address);
    }

    public override string ToString() => Value ?? string.Empty;
}
