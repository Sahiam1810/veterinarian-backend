namespace Domain.Common.Security;

public static class SystemRoles
{
    public const string Admin = "Administrador";
    public const string Veterinarian = "Veterinario";
    public const string Receptionist = "Recepcionista";
    public const string Assistant = "Auxiliar";
    public const string Client = "Cliente";

    public static readonly Guid AdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid VeterinarianId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid ReceptionistId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid AssistantId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    public static readonly Guid ClientId = Guid.Parse("77777777-7777-7777-7777-777777777777");
}
