using MediatR;

namespace Application.Security.ChangePassword;

public sealed record ChangeMyPasswordCommand(
    Guid UserAccountId,
    string CurrentPassword,
    string NewPassword) : IRequest;
