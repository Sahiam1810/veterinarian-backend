namespace Domain.Notifications.ValueObjects;

public sealed record NotificationType
{
    public const int MaxLength = 20;

    private NotificationType(string value) => Value = value;

    public string Value { get; }

    public static NotificationType Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "El tipo de notificación es obligatorio.",
                nameof(value));
        }

        var type = value.Trim();

        if (type.Length > MaxLength)
        {
            throw new ArgumentException(
                $"El tipo de notificación no puede superar los {MaxLength} caracteres.",
                nameof(value));
        }

        return new NotificationType(type);
    }

    public override string ToString() => Value;
}
