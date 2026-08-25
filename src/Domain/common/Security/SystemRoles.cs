namespace Domain.Common.Security;

public static class SystemRoles
{
    public const string Admin = "Administrator";
    public const string Agent = "Agent";
    public const string Client = "Client";

    public static readonly Guid AdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid AgentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid ClientId = Guid.Parse("33333333-3333-3333-3333-333333333333");
}
