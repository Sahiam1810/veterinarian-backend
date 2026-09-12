using Application.Common.Results;

namespace Application.Telegram.Registration;

// Codes estables de alta Telegram. El bot traduce por code; Description neutra (no UX ES).
public static class TelegramRegistrationErrors
{
    private const string GenericDescription = "Telegram registration failed.";

    public static readonly Error InvalidOrExpired = new(
        "Telegram.Registration.InvalidOrExpired",
        GenericDescription);

    public static readonly Error IdentityConflict = new(
        "Telegram.Registration.IdentityConflict",
        GenericDescription);
}
