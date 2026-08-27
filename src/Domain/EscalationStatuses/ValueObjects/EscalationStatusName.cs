namespace Domain.EscalationStatuses.ValueObjects;

// Value object del nombre del estado de escalamiento.
public sealed record EscalationStatusName
{
    public const int MaxLength = 50;

    private EscalationStatusName(string value) => Value = value;

    public string Value { get; }

    // Valida y normaliza el nombre del estado.
    public static EscalationStatusName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El nombre del estado de escalamiento es obligatorio.", nameof(value));

        var name = value.Trim();

        if (name.Length > MaxLength)
            throw new ArgumentException($"El nombre no puede superar los {MaxLength} caracteres.", nameof(value));

        return new EscalationStatusName(name);
    }

    public override string ToString() => Value;
}
