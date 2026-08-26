namespace Domain.Species.ValueObjects;

public sealed record SpeciesName
{
    public const int MaxLength = 20;

    private SpeciesName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static SpeciesName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "El nombre de la especie es obligatorio.",
                nameof(value));
        }

        var speciesName = value.Trim();

        if (speciesName.Length > MaxLength)
        {
            throw new ArgumentException(
                $"El nombre de la especie no puede superar los {MaxLength} caracteres.",
                nameof(value));
        }

        return new SpeciesName(speciesName);
    }

    public override string ToString() => Value;
}
