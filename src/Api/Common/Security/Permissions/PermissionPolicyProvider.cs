using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Api.Common.Security.Permissions;

// Resuelve policies dinámicas con prefijo "perm:" (ver RequirePermissionAttribute)
// y delega cualquier otro nombre de policy (AdminOnly, StaffOnly, etc.) al
// provider por defecto, para poder migrar los controllers de forma gradual.
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(RequirePermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            return _fallbackProvider.GetPolicyAsync(policyName);
        }

        var remainder = policyName[RequirePermissionAttribute.PolicyPrefix.Length..];
        var separatorIndex = remainder.LastIndexOf(':');

        if (separatorIndex <= 0
            || !Enum.TryParse<PermissionAction>(remainder[(separatorIndex + 1)..], out var action))
        {
            return Task.FromResult<AuthorizationPolicy?>(null);
        }

        var moduleName = remainder[..separatorIndex];

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(moduleName, action))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        => _fallbackProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        => _fallbackProvider.GetFallbackPolicyAsync();
}
