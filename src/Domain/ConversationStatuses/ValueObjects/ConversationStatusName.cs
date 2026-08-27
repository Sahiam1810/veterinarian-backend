namespace Domain.ConversationStatuses.ValueObjects;

// Value object del nombre del estado de conversación.
public sealed record ConversationStatusName
{
    public const int MaxLength = 50;

    private ConversationStatusName(string value) => Value = value;

    public string Value { get; }

    // Valida y normaliza el nombre del estado.
    public static ConversationStatusName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El nombre del estado de conversación es obligatorio.", nameof(value));

        var name = value.Trim();

        if (name.Length > MaxLength)
            throw new ArgumentException($"El nombre no puede superar los {MaxLength} caracteres.", nameof(value));

        return new ConversationStatusName(name);
    }

    public override string ToString() => Value;
}
