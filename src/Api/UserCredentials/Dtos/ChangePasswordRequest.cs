namespace Api.UserCredentials.Dtos;

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);
