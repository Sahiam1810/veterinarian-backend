using Application.Common.Results;
using Application.Clients.Errors;
using Application.Security.Errors;

namespace Application.Owners.Errors;

// Códigos estables del núcleo RegisterOwner (front traduce `code`).
public static class OwnerRegistrationErrors
{
    private const string GenericDescription = "Owner registration failed.";

    public static readonly Error EmailAlreadyInUse = new(
        ClientErrorCodes.EmailAlreadyInUse,
        GenericDescription);

    public static readonly Error IdentificationAlreadyInUse = new(
        ClientErrorCodes.IdentificationAlreadyInUse,
        GenericDescription);

    public static readonly Error PhoneAlreadyInUse = new(
        ClientErrorCodes.PhoneAlreadyInUse,
        GenericDescription);

    public static readonly Error ProofRequired = new(
        "OwnerRegistration.ProofRequired",
        GenericDescription);

    public static readonly Error ProofEmailMismatch = new(
        "OwnerRegistration.ProofEmailMismatch",
        GenericDescription);

    public static readonly Error ProofPurposeInvalid = new(
        "OwnerRegistration.ProofPurposeInvalid",
        GenericDescription);

}
