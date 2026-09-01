using MediatR;

namespace Application.Users.UseCase;

public sealed record DeactivateUserCommand(Guid Id) : IRequest;
