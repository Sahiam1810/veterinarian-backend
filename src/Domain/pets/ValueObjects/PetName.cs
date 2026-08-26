namespace Domain.Pets.ValueObjects;

public sealed record PetName
{
    public const int MaxLength = 50;

    private PetName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static PetName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El nombre de la mascota es obligatorio.", nameof(value));

        var name = value.Trim();

        if (name.Length > MaxLength)
            throw new ArgumentException($"El nombre no puede superar los {MaxLength} caracteres.", nameof(value));

        return new PetName(name);
    }

    public override string ToString() => Value;
}
