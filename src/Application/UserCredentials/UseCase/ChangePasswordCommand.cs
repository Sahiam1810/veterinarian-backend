using MediatR;

namespace Application.UserCredentials.UseCase;

public sealed record ChangePasswordCommand(
    Guid Id,
    string CurrentPassword,
    string NewPassword) : IRequest;
