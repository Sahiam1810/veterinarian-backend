namespace Domain.Clients.ValueObjects;

public sealed record ClientFullName
{
    public const int MaxLength = 150;

    private ClientFullName(string value) => Value = value;

    public string Value { get; }

    public static ClientFullName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("El nombre completo es obligatorio.", nameof(value));
        }

        var fullName = value.Trim();
        if (fullName.Length > MaxLength)
        {
            throw new ArgumentException($"El nombre completo no puede superar los {MaxLength} caracteres.", nameof(value));
        }

        return new ClientFullName(fullName);
    }

    public override string ToString() => Value;
}
