namespace Domain.ClientsPets.ValueObjects;

public sealed record PrimaryOwner
{
    private PrimaryOwner(bool value) => Value = value;
    public bool Value { get; }
    public static PrimaryOwner Create(bool value) => new(value);
}
