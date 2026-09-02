using Application.Common.Results;

namespace Application.Telegram.Registration;

public static class TelegramRegistrationErrors
{
    public static readonly Error InvalidOrExpired = new(
        "Telegram.Registration.InvalidOrExpired",
        "El enlace de registro es inválido, ya fue utilizado o venció.");

    public static readonly Error IdentityConflict = new(
        "Telegram.Registration.IdentityConflict",
        "El chat ya está vinculado a otra identidad.");
}
