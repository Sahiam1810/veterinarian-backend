namespace Domain.ChatUserProfiles.ValueObjects;

/// <summary>
/// URL absoluta opcional del avatar del perfil de chat.
/// </summary>
public sealed record ProfileAvatarUrl
{
    public const int MaxLength = 500;

    private ProfileAvatarUrl(string? value)
    {
        Value = value;
    }

    public string? Value { get; }

    /// <summary>
    /// Crea la URL del avatar. Nulo es válido; si se informa, debe ser una URL absoluta.
    /// </summary>
    public static ProfileAvatarUrl Create(string? value)
    {
        if (value is null)
        {
            return new ProfileAvatarUrl((string?)null);
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "La URL del avatar no puede estar vacía ni contener solo espacios.",
                nameof(value));
        }

        var avatarUrl = value.Trim();
        if (avatarUrl.Length > MaxLength)
        {
            throw new ArgumentException(
                $"La URL del avatar no puede superar los {MaxLength} caracteres.",
                nameof(value));
        }

        if (!Uri.TryCreate(avatarUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException(
                "La URL del avatar debe ser una URL absoluta válida.",
                nameof(value));
        }

        return new ProfileAvatarUrl(avatarUrl);
    }

    public override string ToString() => Value ?? string.Empty;
}
