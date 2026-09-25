namespace Domain.Pets.ValueObjects;

public sealed record PetGender
{
    public static readonly string Male = "M";
    public static readonly string Female = "F";
    public static readonly string Unspecified = "U";

    private static readonly HashSet<string> ValidValues = [Male, Female, Unspecified];

    private PetGender(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static PetGender Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El género de la mascota es obligatorio.", nameof(value));

        var gender = value.Trim().ToUpperInvariant();

        if (!ValidValues.Contains(gender))
            throw new ArgumentException($"El género debe ser '{Male}' (macho), '{Female}' (hembra) o '{Unspecified}' (desconocido).", nameof(value));

        return new PetGender(gender);
    }

    public override string ToString() => Value;
}
