using MediatR;

namespace Application.Security.ChangePassword;

public sealed record ChangeMyPasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword) : IRequest;
