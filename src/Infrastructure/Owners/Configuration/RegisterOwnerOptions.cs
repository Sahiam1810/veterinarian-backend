namespace Infrastructure.Owners.Configuration;

// RequireContactProofs aplica solo a staff. Bot/Telegram siempre consumen proof Email.
public sealed class RegisterOwnerOptions
{
    public const string SectionName = "RegisterOwner";

    public bool RequireContactProofs { get; init; }
}
