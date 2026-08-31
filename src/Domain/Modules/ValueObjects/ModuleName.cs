namespace Domain.Modules.ValueObjects;

public sealed record ModuleName
{
    public const int MaxLength = 50;

    private ModuleName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ModuleName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "El nombre del módulo es obligatorio.",
                nameof(value));
        }

        var moduleName = value.Trim();

        if (moduleName.Length > MaxLength)
        {
            throw new ArgumentException(
                $"El nombre del módulo no puede superar los {MaxLength} caracteres.",
                nameof(value));
        }

        return new ModuleName(moduleName);
    }

    public override string ToString() => Value;
}
