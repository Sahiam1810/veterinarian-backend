namespace Domain.Pets.ValueObjects;

public sealed record PetWeight
{
    public const decimal Min = 0.01m;
    public const decimal Max = 500m;

    private PetWeight(decimal value)
    {
        Value = value;
    }

    public decimal Value { get; }

    public static PetWeight Create(decimal value)
    {
        if (value < Min || value > Max)
            throw new ArgumentException($"El peso debe estar entre {Min} y {Max} kg.", nameof(value));

        return new PetWeight(value);
    }

    public override string ToString() => Value.ToString("F2");
}
