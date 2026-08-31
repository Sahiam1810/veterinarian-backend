namespace Domain.Specialties.ValueObjects;

public sealed record SpecialtyName
{
    public const int MaxLength = 120;
    private SpecialtyName(string value) => Value = value;
    public string Value { get; }

    public static SpecialtyName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El nombre de la especialidad es obligatorio.", nameof(value));

        var name = value.Trim();
        if (name.Length > MaxLength)
            throw new ArgumentException($"El nombre no puede superar los {MaxLength} caracteres.", nameof(value));

        return new SpecialtyName(name);
    }

    public override string ToString() => Value;
}
