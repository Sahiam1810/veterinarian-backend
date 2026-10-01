namespace Domain.Users.ValueObjects;

// URL absoluta opcional de la foto de perfil (USERS.PHOTO_URL).
public sealed record UserPhotoUrl
{
    public const int MaxLength = 500;

    private UserPhotoUrl(string? value)
    {
        Value = value;
    }

    public string? Value { get; }

    // Nulo/vacío = sin foto; si hay valor, debe ser http o https.
    public static UserPhotoUrl Create(string? value)
    {
        if (value is null)
            return new UserPhotoUrl((string?)null);

        var photoUrl = value.Trim();
        if (photoUrl.Length == 0)
            return new UserPhotoUrl((string?)null);

        if (photoUrl.Length > MaxLength)
            throw new ArgumentException(
                $"La URL de la foto no puede superar los {MaxLength} caracteres.",
                nameof(value));

        if (!Uri.TryCreate(photoUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException(
                "La URL de la foto debe ser una dirección http o https válida.",
                nameof(value));
        }

        return new UserPhotoUrl(photoUrl);
    }

    public override string ToString() => Value ?? string.Empty;
}
