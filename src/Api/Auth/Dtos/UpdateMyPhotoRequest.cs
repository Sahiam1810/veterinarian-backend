namespace Api.Auth.Dtos;

// Enlace http(s) de la foto de perfil. Vacío o nulo la quita.
public sealed record UpdateMyPhotoRequest(string? PhotoUrl);
