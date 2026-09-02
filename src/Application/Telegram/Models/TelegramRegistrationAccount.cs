using Domain.Telegram.Enums;

namespace Application.Telegram.Models;

public sealed record TelegramRegistrationAccount(
    TelegramRegistrationAccountKind Kind,
    Guid? PersonId,
    string NormalizedEmail);
