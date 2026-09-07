namespace Api.Auth.Dtos;

public sealed record RequestEmailOtpResponse(
    string Message = "Si el correo electrónico está registrado, se enviará un código de verificación.");
