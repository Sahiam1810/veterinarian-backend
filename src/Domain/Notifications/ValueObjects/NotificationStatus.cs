namespace Domain.Notifications.ValueObjects;

public sealed record NotificationStatus
{
    public const int MaxLength = 20;

    private NotificationStatus(string value) => Value = value;

    public string Value { get; }

    public static NotificationStatus Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "El estado de la notificación es obligatorio.",
                nameof(value));
        }

        var status = value.Trim();

        if (status.Length > MaxLength)
        {
            throw new ArgumentException(
                $"El estado no puede superar los {MaxLength} caracteres.",
                nameof(value));
        }

        return new NotificationStatus(status);
    }

    public override string ToString() => Value;
}
