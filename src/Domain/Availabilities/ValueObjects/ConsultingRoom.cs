namespace Domain.Availabilities.ValueObjects;

// Sala fisica opcional; el JSON publico usa consultingRoom, no consultorio.
public sealed record ConsultingRoom
{
    public const int MaxLength = 50;

    private ConsultingRoom(string value) => Value = value;

    public string Value { get; }

    public static ConsultingRoom? CreateOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var room = value.Trim();
        if (room.Length > MaxLength)
        {
            throw new ArgumentException(
                $"El consultorio no puede superar los {MaxLength} caracteres.",
                nameof(value));
        }

        return new ConsultingRoom(room);
    }

    public override string ToString() => Value;
}
