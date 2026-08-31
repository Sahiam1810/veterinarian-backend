namespace Domain.Specialties.ValueObjects;

public sealed record SpecialtyDescription
{
    public const int MaxLength = 120;
    private SpecialtyDescription(string? value) => Value = value;
    public string? Value { get; }

    public static SpecialtyDescription Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return new SpecialtyDescription((string?)null);
        var description = value.Trim();
        if (description.Length > MaxLength)
            throw new ArgumentException($"La descripción no puede superar los {MaxLength} caracteres.", nameof(value));
        return new SpecialtyDescription(description);
    }

    public override string? ToString() => Value;
}
