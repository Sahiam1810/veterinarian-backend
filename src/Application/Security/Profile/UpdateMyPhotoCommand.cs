using MediatR;

namespace Application.Security.Profile;

// Actualiza la foto de perfil del usuario autenticado. Vacío quita la foto.
public sealed record UpdateMyPhotoCommand(
    Guid UserAccountId,
    string? PhotoUrl) : IRequest;
