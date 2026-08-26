using System.Text.RegularExpressions;

namespace Domain.UserAccounts.ValueObjects;

public sealed record AccountMail
{
    public const int MaxLength = 150;

    private static readonly Regex Pattern = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled);

    private AccountMail(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static AccountMail Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "El correo de la cuenta es obligatorio.",
                nameof(value));
        }

        var mail = value.Trim().ToLowerInvariant();

        if (mail.Length > MaxLength)
        {
            throw new ArgumentException(
                $"El correo de la cuenta no puede superar los {MaxLength} caracteres.",
                nameof(value));
        }

        if (!Pattern.IsMatch(mail))
        {
            throw new ArgumentException(
                "El correo de la cuenta no tiene un formato válido.",
                nameof(value));
        }

        return new AccountMail(mail);
    }

    public override string ToString() => Value;
}
