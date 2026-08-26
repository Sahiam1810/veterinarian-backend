namespace Domain.Races.ValueObjects;

public sealed record RaceName
{
    public const int MaxLength = 20;

    private RaceName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static RaceName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "El nombre de la raza es obligatorio.",
                nameof(value));
        }

        var raceName = value.Trim();

        if (raceName.Length > MaxLength)
        {
            throw new ArgumentException(
                $"El nombre de la raza no puede superar los {MaxLength} caracteres.",
                nameof(value));
        }

        return new RaceName(raceName);
    }

    public override string ToString() => Value;
}
