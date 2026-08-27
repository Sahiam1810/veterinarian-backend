namespace Domain.Priorities.ValueObjects;

// Value object del nombre de prioridad.
public sealed record PriorityName
{
    public const int MaxLength = 50;

    private PriorityName(string value) => Value = value;

    public string Value { get; }

    // Valida y normaliza el nombre de la prioridad.
    public static PriorityName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El nombre de la prioridad es obligatorio.", nameof(value));

        var name = value.Trim();

        if (name.Length > MaxLength)
            throw new ArgumentException($"El nombre no puede superar los {MaxLength} caracteres.", nameof(value));

        return new PriorityName(name);
    }

    public override string ToString() => Value;
}
