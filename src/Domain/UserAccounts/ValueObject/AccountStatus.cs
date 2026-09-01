namespace Domain.UserAccounts.ValueObjects;

// Único origen de verdad para los valores permitidos de UserAccounts.Status.
// AuthenticationService.IsActiveAccount compara contra el literal "Activo":
// antes de esto, Status era texto libre y un typo ("activo", "Active") dejaba
// una cuenta bloqueada sin ningún error de validación que lo avisara.
public static class AccountStatus
{
    public const string Active = "Activo";

    public const string Inactive = "Inactivo";

    public static readonly IReadOnlyCollection<string> AllowedValues = [Active, Inactive];
}
