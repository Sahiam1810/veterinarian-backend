using System.ComponentModel.DataAnnotations;

namespace Api.Auth.Dtos;

public sealed record ConfirmEmailOtpRequest(
    [property: Required]
    [property: EmailAddress]
    string Email,
    [property: Required]
    [property: StringLength(6, MinimumLength = 6)]
    string Code);
