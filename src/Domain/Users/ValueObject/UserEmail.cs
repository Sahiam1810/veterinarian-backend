using System.Text.RegularExpressions;

namespace Domain.Users.ValueObjects;

public sealed record UserEmail
{
    public const int MaxLength = 150;

    private static readonly Regex Pattern = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled);

    private UserEmail(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static UserEmail Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "El correo electrónico es obligatorio.",
                nameof(value));
        }

        var email = value.Trim().ToLowerInvariant();

        if (email.Length > MaxLength)
        {
            throw new ArgumentException(
                $"El correo electrónico no puede superar los {MaxLength} caracteres.",
                nameof(value));
        }

        if (!Pattern.IsMatch(email))
        {
            throw new ArgumentException(
                "El correo electrónico no tiene un formato válido.",
                nameof(value));
        }

        return new UserEmail(email);
    }

    public override string ToString() => Value;
}
