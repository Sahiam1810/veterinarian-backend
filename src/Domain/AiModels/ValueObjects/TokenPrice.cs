namespace Domain.AiModels.ValueObjects;

public sealed record TokenPrice
{
    private TokenPrice(decimal value)
    {
        Value = value;
    }

    public decimal Value { get; }

    public static TokenPrice Create(decimal value)
    {
        if (value < 0)
        {
            throw new ArgumentException("El precio por token no puede ser negativo.", nameof(value));
        }

        return new TokenPrice(value);
    }

    public override string ToString() => Value.ToString();
}
