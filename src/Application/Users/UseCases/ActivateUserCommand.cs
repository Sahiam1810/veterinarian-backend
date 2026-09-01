using MediatR;

namespace Application.Users.UseCase;

public sealed record ActivateUserCommand(Guid Id) : IRequest;
