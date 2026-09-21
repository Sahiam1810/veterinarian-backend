using Application.Clients.Abstraction;
using Application.Security;
using Application.Security.Claims;
using Application.Security.Models;
using Application.Telegram.Abstractions;
using Application.Telegram.Errors;
using Application.Telegram.Models;
using Domain.Roles;
using Infrastructure.Security.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Telegram.Security;

public sealed class AgentDelegatedIdentityProvider(
    IClientRepository clientsRepository,
    ITelegramRuntimeSettings settings,
    JwtTokenIssuer tokenIssuer) : IAgentDelegatedIdentityProvider
{
    private const string GuestRole = "TelegramGuest";

    public AgentDelegatedIdentity GetGuest(long telegramUserId)
    {
        if (telegramUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(telegramUserId));
        }

        var accountId = DeterministicId("account", telegramUserId);
        var personId = DeterministicId("person", telegramUserId);
        var roleId = DeterministicId("role", 1);
        var identity = new AuthenticatedIdentity(
            accountId,
            personId,
            roleId,
            GuestRole,
            "Telegram Guest",
            "telegram_guest",
            "guest@telegram.invalid",
            "Invitado");
        var token = tokenIssuer.Issue(
            identity,
            settings.DelegatedTokenLifetime,
            permissions: [],
            extraClaims: [new Claim(DelegatedTokenClaims.TelegramUserId, telegramUserId.ToString())]);
        return new AgentDelegatedIdentity(personId, GuestRole, token.Token);
    }

    public async Task<AgentDelegatedIdentity> GetAsync(
        Guid clientId,
        CancellationToken cancellationToken)
    {
        var client = await clientsRepository.GetByIdAsync(clientId, cancellationToken);
        if (client is null || !client.IsActive)
        {
            throw new TelegramAccountUnavailableException();
        }

        var roleId = SystemRoles.ClientRoleId;
        var roleName = WebPlatformAccess.ClientRoleName;
        var email = client.Email.Value;
        var username = client.Email.Value;

        var identity = new AuthenticatedIdentity(
            client.Id,
            client.Id,
            roleId,
            roleName,
            client.FullName.Value,
            username,
            email,
            "Activo");

        var token = tokenIssuer.IssueDelegated(
            identity,
            settings.DelegatedTokenLifetime,
            Array.Empty<string>(),
            DelegatedTokenClaims.TelegramAgent);

        return new AgentDelegatedIdentity(client.Id, roleName, token.Token);
    }

    private static Guid DeterministicId(string scope, long externalId)
    {
        var hash = SHA256.HashData(
            Encoding.UTF8.GetBytes($"huellitas:telegram:guest:{scope}:{externalId}"));
        return new Guid(hash.AsSpan(0, 16));
    }
}
