using MediatR;

namespace Application.Users.UseCase;

// Exclusivo de SuperAdmin: recuperación de acceso para otro usuario, sin
// requerir su contraseña actual (a diferencia de ChangeMyPasswordCommand,
// que es el autoservicio de PATCH /api/auth/me/password).
public sealed record ResetUserPasswordCommand(
    Guid Id,
    string NewPassword) : IRequest;
