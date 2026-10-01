using Application.Common.Results;

namespace Application.ContactVerification.Errors;

// Catálogo estable para el front (traduce `code`, no Description).
public static class ContactVerificationErrors
{
    private const string GenericDescription = "Contact verification failed.";

    public static readonly Error InvalidCode = new(
        "ContactVerification.InvalidCode",
        GenericDescription);

    public static readonly Error Expired = new(
        "ContactVerification.Expired",
        GenericDescription);

    public static readonly Error Blocked = new(
        "ContactVerification.Blocked",
        GenericDescription);

    public static readonly Error ResendTooSoon = new(
        "ContactVerification.ResendTooSoon",
        GenericDescription);

    public static readonly Error SessionNotFound = new(
        "ContactVerification.SessionNotFound",
        GenericDescription);

    public static readonly Error ProofInvalid = new(
        "ContactVerification.ProofInvalid",
        GenericDescription);

    public static readonly Error ProofAlreadyConsumed = new(
        "ContactVerification.ProofAlreadyConsumed",
        GenericDescription);

    public static readonly Error ProofExpired = new(
        "ContactVerification.ProofExpired",
        GenericDescription);

    public static readonly Error ChannelNotSupported = new(
        "ContactVerification.ChannelNotSupported",
        GenericDescription);

    public static readonly Error PurposeInvalid = new(
        "ContactVerification.PurposeInvalid",
        GenericDescription);

    // Correo con formato inválido en Request Email (3.1).
    public static readonly Error EmailInvalid = new(
        "ContactVerification.EmailInvalid",
        GenericDescription);

    // Fallo del dispatcher SMTP (sin filtrar detalles del proveedor).
    public static readonly Error DeliveryFailed = new(
        "ContactVerification.DeliveryFailed",
        GenericDescription);

    public static readonly Error NotImplemented = new(
        "ContactVerification.NotImplemented",
        GenericDescription);
}
