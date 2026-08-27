namespace Domain.AccountStatements.ValueObjects;

public sealed record StatementStatus
{
    public const int MaxLength = 30;

    private StatementStatus(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static StatementStatus Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "El estado del estado de cuenta es obligatorio.",
                nameof(value));
        }

        var status = value.Trim();

        if (status.Length > MaxLength)
        {
            throw new ArgumentException(
                $"El estado del estado de cuenta no puede superar los {MaxLength} caracteres.",
                nameof(value));
        }

        return new StatementStatus(status);
    }

    public override string ToString() => Value;
}
