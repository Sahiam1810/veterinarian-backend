namespace Infrastructure.Messaging.Configuration;

// Configuración del proveedor Twilio (SMS / WhatsApp).
public sealed class TwilioOptions
{
    public const string SectionName = "Twilio";

    public bool Enabled { get; init; }

    public string AccountSid { get; init; } = string.Empty;

    public string AuthToken { get; init; } = string.Empty;

    // Número o Messaging Service SID en formato E.164 / MG...
    public string FromNumber { get; init; } = string.Empty;

    // Prefijo WhatsApp: whatsapp:+57...
    public string? WhatsAppFrom { get; init; }

    // Prefijo de país si el destino llega solo con dígitos nacionales (ej. 57).
    public string DefaultCountryCode { get; init; } = "57";
}
