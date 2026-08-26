namespace Domain.Pets.ValueObjects;

public sealed record PetObservations
{
    public const int MaxLength = 500;

    private PetObservations(string? value)
    {
        Value = value;
    }

    public string? Value { get; }

    public static PetObservations Create(string? value)
    {
        if (value is null)
            return new PetObservations((string?)null);

        var observations = value.Trim();

        if (observations.Length > MaxLength)
            throw new ArgumentException($"Las observaciones no pueden superar los {MaxLength} caracteres.", nameof(value));

        return new PetObservations(observations.Length == 0 ? null : observations);
    }

    public override string ToString() => Value ?? string.Empty;
}
