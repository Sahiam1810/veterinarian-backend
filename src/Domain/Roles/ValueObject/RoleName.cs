namespace HelpDesk.Domain.Roles.ValueObjects;

public sealed record RoleName
{
    public const int MaxLength = 50;

    private RoleName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static RoleName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "El nombre del rol es obligatorio.",
                nameof(value));
        }

        var roleName = value.Trim();

        if (roleName.Length > MaxLength)
        {
            throw new ArgumentException(
                $"El nombre del rol no puede superar los {MaxLength} caracteres.",
                nameof(value));
        }

        return new RoleName(roleName);
    }

    public override string ToString() => Value;
}