namespace Domain.UserAccounts.ValueObjects;

public sealed record AccountUsername
{
    public const int MaxLength = 30;

    private AccountUsername(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static AccountUsername Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "El nombre de usuario es obligatorio.",
                nameof(value));
        }

        var username = value.Trim();

        if (username.Length > MaxLength)
        {
            throw new ArgumentException(
                $"El nombre de usuario no puede superar los {MaxLength} caracteres.",
                nameof(value));
        }

        return new AccountUsername(username);
    }

    public override string ToString() => Value;
}
