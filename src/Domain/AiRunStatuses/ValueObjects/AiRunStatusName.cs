namespace Domain.AiRunStatuses.ValueObjects;

public sealed record AiRunStatusName
{
    public const int MaxLength = 50;

    private AiRunStatusName(string value) => Value = value;

    public string Value { get; }

    public static AiRunStatusName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El nombre del estado de texto es obligatorio.", nameof(value));

        var name = value.Trim();

        if (name.Length > MaxLength)
            throw new ArgumentException($"El nombre no puede superar los {MaxLength} caracteres.", nameof(value));

        return new AiRunStatusName(name);
    }

    public override string ToString() => Value;
}
