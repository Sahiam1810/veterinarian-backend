namespace Domain.Clients.ValueObjects;

public sealed record ClientIdentificationNumber
{
    public const int MaxLength = 20;

    private ClientIdentificationNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ClientIdentificationNumber Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "El número de identificación es obligatorio.",
                nameof(value));
        }

        var identification = value.Trim();

        if (identification.Length > MaxLength)
        {
            throw new ArgumentException(
                $"El número de identificación no puede superar los {MaxLength} caracteres.",
                nameof(value));
        }

        return new ClientIdentificationNumber(identification);
    }

    public override string ToString() => Value;
}
