using MediatR;

namespace Application.Users.UseCase;

// Elimina de forma permanente un usuario inactivo y sus perfiles asociados.
public sealed record DeleteUserCommand(Guid Id) : IRequest;
