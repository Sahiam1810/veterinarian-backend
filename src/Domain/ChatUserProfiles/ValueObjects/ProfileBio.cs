namespace Domain.ChatUserProfiles.ValueObjects;

/// <summary>
/// Biografía opcional del perfil de chat.
/// </summary>
public sealed record ProfileBio
{
    public const int MaxLength = 500;

    private ProfileBio(string? value)
    {
        Value = value;
    }

    public string? Value { get; }

    /// <summary>
    /// Crea la biografía. Nulo es válido; si se informa, no puede ser solo espacios.
    /// </summary>
    public static ProfileBio Create(string? value)
    {
        if (value is null)
        {
            return new ProfileBio((string?)null);
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "La biografía no puede estar vacía ni contener solo espacios.",
                nameof(value));
        }

        var bio = value.Trim();
        if (bio.Length > MaxLength)
        {
            throw new ArgumentException(
                $"La biografía no puede superar los {MaxLength} caracteres.",
                nameof(value));
        }

        return new ProfileBio(bio);
    }

    public override string ToString() => Value ?? string.Empty;
}
