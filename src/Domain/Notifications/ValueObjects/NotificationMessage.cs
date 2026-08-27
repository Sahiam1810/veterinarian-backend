namespace Domain.Notifications.ValueObjects;

public sealed record NotificationMessage
{
    public const int MaxLength = 1000;

    private NotificationMessage(string value) => Value = value;

    public string Value { get; }

    public static NotificationMessage Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "El mensaje de la notificación es obligatorio.",
                nameof(value));
        }

        var message = value.Trim();

        if (message.Length > MaxLength)
        {
            throw new ArgumentException(
                $"El mensaje no puede superar los {MaxLength} caracteres.",
                nameof(value));
        }

        return new NotificationMessage(message);
    }

    public override string ToString() => Value;
}
