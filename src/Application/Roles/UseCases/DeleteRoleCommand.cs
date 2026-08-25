using MediatR;

namespace HelpDesk.Application.Roles.UseCase;

public sealed record DeleteRoleCommand(Guid Id) : IRequest<bool>;
