namespace Api.Auth.Dtos;

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);
