using Application.Common.Results;

namespace Application.Security.Errors;

// Codigos estables del portal Cliente retirado. El front traduce por code.
public static class ClientPortalErrors
{
    private const string GoneDescription = "Client portal route has been retired.";

    // 410: ruta de portal Cliente retirada; usar chatbot/staff.
    public static readonly Error Gone = new(
        "ClientPortal.Gone",
        GoneDescription);
}