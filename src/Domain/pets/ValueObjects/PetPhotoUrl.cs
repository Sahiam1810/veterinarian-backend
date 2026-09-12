namespace Domain.Pets.ValueObjects;

// URL absoluta opcional de la foto de la mascota (persistida en PETS.PHOTO_URL).
public sealed record PetPhotoUrl
{
    public const int MaxLength = 500;

    private PetPhotoUrl(string? value)
    {
        Value = value;
    }

    public string? Value { get; }

    // Nulo/vacío = sin foto; si hay valor, debe ser URL absoluta http(s).
    public static PetPhotoUrl Create(string? value)
    {
        if (value is null)
            return new PetPhotoUrl((string?)null);

        var photoUrl = value.Trim();
        if (photoUrl.Length == 0)
            return new PetPhotoUrl((string?)null);

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

        return new PetPhotoUrl(photoUrl);
    }

    public override string ToString() => Value ?? string.Empty;
}
