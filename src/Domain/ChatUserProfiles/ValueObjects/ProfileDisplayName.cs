namespace Domain.ChatUserProfiles.ValueObjects;

/// <summary>
/// Nombre visible opcional del perfil de chat.
/// </summary>
public sealed record ProfileDisplayName
{
    public const int MaxLength = 150;

    private ProfileDisplayName(string? value)
    {
        Value = value;
    }

    public string? Value { get; }

    /// <summary>
    /// Crea el nombre visible. Nulo es válido; si se informa, no puede ser solo espacios.
    /// </summary>
    public static ProfileDisplayName Create(string? value)
    {
        if (value is null)
        {
            return new ProfileDisplayName((string?)null);
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "El nombre visible no puede estar vacío ni contener solo espacios.",
                nameof(value));
        }

        var displayName = value.Trim();
        if (displayName.Length > MaxLength)
        {
            throw new ArgumentException(
                $"El nombre visible no puede superar los {MaxLength} caracteres.",
                nameof(value));
        }

        return new ProfileDisplayName(displayName);
    }

    public override string ToString() => Value ?? string.Empty;
}
