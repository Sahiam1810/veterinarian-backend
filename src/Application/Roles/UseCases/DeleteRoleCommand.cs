using MediatR;

namespace Application.Roles.UseCase;

public sealed record DeleteRoleCommand(Guid Id) : IRequest;
