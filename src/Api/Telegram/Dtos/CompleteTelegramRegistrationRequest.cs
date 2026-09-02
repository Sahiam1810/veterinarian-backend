using System.ComponentModel.DataAnnotations;

namespace Api.Telegram.Dtos;

public sealed record CompleteTelegramRegistrationRequest(
    [property: Required, MaxLength(150)] string FullName,
    [property: Required, MaxLength(20)] string IdentificationNumber,
    [property: Required, MaxLength(50)] string UserName,
    [property: Required, MinLength(8), MaxLength(100), DataType(DataType.Password)] string Password,
    [property: Required, DataType(DataType.Password), Compare("Password")] string PasswordConfirmation);
