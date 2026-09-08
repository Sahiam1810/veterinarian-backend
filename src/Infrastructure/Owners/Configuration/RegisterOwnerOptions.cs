namespace Infrastructure.Owners.Configuration;

// RequireContactProofs aplica solo a staff. Bot siempre ConsumeProof; Telegram usa equivalencia de sesión.
public sealed class RegisterOwnerOptions
{
    public const string SectionName = "RegisterOwner";

    public bool RequireContactProofs { get; init; }
}
