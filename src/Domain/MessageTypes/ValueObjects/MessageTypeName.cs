namespace Domain.MessageTypes.ValueObjects;

public sealed record MessageTypeName
{
    public const int MaxLength = 50;

    private MessageTypeName(string value) => Value = value;

    public string Value { get; }

    public static MessageTypeName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El nombre del tipo de mensaje es obligatorio.", nameof(value));

        var name = value.Trim();

        if (name.Length > MaxLength)
            throw new ArgumentException($"El nombre no puede superar los {MaxLength} caracteres.", nameof(value));

        return new MessageTypeName(name);
    }

    public override string ToString() => Value;
}
