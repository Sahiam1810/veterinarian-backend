namespace Application.Owners.Enums;

// Canal de alta de dueño. v1 no incluye WhatsApp ni SMS de contacto.
public enum RegisterOwnerChannel
{
    Staff = 1,
    Bot = 2,
    Telegram = 3
}
