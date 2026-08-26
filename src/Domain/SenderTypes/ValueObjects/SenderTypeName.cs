namespace Domain.SenderTypes.ValueObjects;

public sealed record SenderTypeName
{
    public const int MaxLength = 50;

    private SenderTypeName(string value) => Value = value;

    public string Value { get; }

    public static SenderTypeName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El nombre del tipo de remitente es obligatorio.", nameof(value));

        var name = value.Trim();

        if (name.Length > MaxLength)
            throw new ArgumentException($"El nombre no puede superar los {MaxLength} caracteres.", nameof(value));

        return new SenderTypeName(name);
    }

    public override string ToString() => Value;
}
