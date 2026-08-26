namespace Domain.AiModels.ValueObjects;

public sealed record TokenLimit
{
    private TokenLimit(int value)
    {
        Value = value;
    }

    public int Value { get; }

    public static TokenLimit Create(int value)
    {
        if (value < 0)
        {
            throw new ArgumentException("El límite de tokens no puede ser negativo.", nameof(value));
        }

        return new TokenLimit(value);
    }

    public override string ToString() => Value.ToString();
}
