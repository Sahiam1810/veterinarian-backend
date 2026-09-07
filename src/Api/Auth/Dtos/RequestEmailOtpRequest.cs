using System.ComponentModel.DataAnnotations;

namespace Api.Auth.Dtos;

public sealed record RequestEmailOtpRequest(
    [property: Required]
    [property: EmailAddress]
    string Email);
