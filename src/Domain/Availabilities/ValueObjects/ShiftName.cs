namespace Domain.Availabilities.ValueObjects;

// Etiqueta de turno (Manana/Tarde); el turno partido sigue siendo dos filas.
public sealed record ShiftName
{
    public const int MaxLength = 30;

    private ShiftName(string value) => Value = value;

    public string Value { get; }

    public static ShiftName? CreateOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var name = value.Trim();
        if (name.Length > MaxLength)
        {
            throw new ArgumentException(
                $"El nombre de turno no puede superar los {MaxLength} caracteres.",
                nameof(value));
        }

        return new ShiftName(name);
    }

    public override string ToString() => Value;
}