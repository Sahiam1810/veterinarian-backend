using System.Security.Claims;
using System.Text.Json;
using Api.Auth.Dtos;
using Api.UserCredentials.Dtos;
using Microsoft.AspNetCore.Http;
using Application.Security.Models;
using Application.Security.Login;
using Application.Security.Refresh;
using Application.Security.Revoke;
using Application.Security.ChangePassword;
using Application.Security.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Api.Common.Security;
using MediatR;
using Application.Security.Profile;
using Application.Permissions.UseCases;
using Application.Modules.UseCases;


namespace Api.Auth.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    // 401/403 de autenticación usan application/problem+json (RFC 7807),
    // igual que JwtResponseEvents — es el mismo tipo de error, no uno de negocio.
    private ContentResult AuthProblem(int status, string title, string code) => new()
    {
        StatusCode = status,
        ContentType = "application/problem+json",
        Content = JsonSerializer.Serialize(new
        {
            type = $"https://httpstatuses.com/{status}",
            title,
            status,
            code
        })
    };

    // Registro público eliminado: el Cliente nunca se loguea (solo
    // interactúa vía chatbot), así que no existe un flujo de auto-registro
    // con contraseña. La vinculación por Telegram sigue su propio flujo
    // (TelegramRegistrationController), que no depende de este endpoint.

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [HttpPost("login")]
    [EndpointSummary("Inicia sesión de usuario")]
    [EndpointDescription("Valida correo y contraseña y genera AccessToken/RefreshToken. Fallo 401: application/problem+json con code fijo Authentication.InvalidCredentials (el front traduce por code).")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new LoginCommand(request.Email, request.Password),
            cancellationToken);

        if (result.IsFailure)
        {
            // PlatformAccessDenied es 403 (rol no admitido), no 401 de credenciales.
            if (result.Error.Code == AuthenticationErrors.PlatformAccessDenied.Code)
            {
                return AuthProblem(
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    result.Error.Code);
            }

            return AuthProblem(StatusCodes.Status401Unauthorized, "Unauthorized", result.Error.Code);
        }

        return Ok(AuthenticationResponse.From(result.Value));
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.Refresh)]
    [HttpPost("refresh")]
    [EndpointSummary("Renueva los tokens JWT vencidos usando el Refresh Token")]
    [EndpointDescription("Genera AccessToken y RefreshToken rotado. Fallo 401: application/problem+json con code fijo Authentication.InvalidRefreshToken.")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RefreshTokenCommand(request.RefreshToken),
            cancellationToken);

        if (result.IsFailure)
        {
            if (result.Error.Code == AuthenticationErrors.PlatformAccessDenied.Code)
            {
                return AuthProblem(
                    StatusCodes.Status403Forbidden,
                    "Forbidden",
                    result.Error.Code);
            }

            return AuthProblem(StatusCodes.Status401Unauthorized, "Unauthorized", result.Error.Code);
        }

        return Ok(AuthenticationResponse.From(result.Value));
    }

    [Authorize]
    [HttpGet("me")]
    [EndpointSummary("Obtiene los datos del usuario autenticado actual")]
    [EndpointDescription("Retorna el identificador, nombre de usuario y correo del usuario correspondiente al token JWT provisto.")]
    [ProducesResponseType(typeof(CurrentProfile), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(subject, out var userAccountId))
        {
            return AuthProblem(
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                AuthenticationErrors.Unauthorized.Code);
        }

        var result = await sender.Send(
            new GetCurrentProfileQuery(userAccountId), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : AuthProblem(
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                AuthenticationErrors.Unauthorized.Code);
    }

    [Authorize]
    [HttpGet("permissions")]
    [EndpointSummary("Obtiene los permisos efectivos del usuario autenticado")]
    [EndpointDescription("Retorna el mapa completo de permisos efectivos del usuario (rol + permisos puntuales) por cada módulo, con sus 4 flags (Ver/Crear/Editar/Eliminar). El SuperAdmin recibe los 4 flags en true para todos los módulos, igual que se salta la matriz de permisos en el resto de la API.")]
    [ProducesResponseType(typeof(UserPermissionsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Permissions(CancellationToken cancellationToken)
    {
        if (User.IsSuperAdmin())
        {
            var modules = await sender.Send(new GetAllModulesQuery(), cancellationToken);

            return Ok(new UserPermissionsResponseDto(
                modules.ToDictionary(
                    module => module.Name.Value,
                    _ => new ModulePermissionDto(true, true, true, true))));
        }

        var roleIdClaim = User.FindFirstValue("role_id");

        if (!Guid.TryParse(roleIdClaim, out var roleId))
        {
            return Unauthorized();
        }

        Guid.TryParse(User.FindFirstValue("person_id"), out var userId);

        var permissions = await sender.Send(
            new GetUserEffectivePermissionsQuery(roleId, userId),
            cancellationToken);

        var dto = new UserPermissionsResponseDto(
            permissions.ToDictionary(
                kvp => kvp.Key,
                kvp => new ModulePermissionDto(
                    kvp.Value.CanView,
                    kvp.Value.CanCreate,
                    kvp.Value.CanEdit,
                    kvp.Value.CanDelete)));

        return Ok(dto);
    }

    [Authorize]
    [HttpPatch("me/password")]
    [EndpointSummary("Cambia la contraseña propia del usuario autenticado")]
    [EndpointDescription("Autoservicio de cambio de contraseña: valida la contraseña actual del usuario autenticado y, si es correcta, la reemplaza por la nueva. Cualquier rol puede usarlo para su propia cuenta; para restablecer la contraseña de otra persona, ver el endpoint exclusivo de SuperAdmin en UserCredentials.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeMyPassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(subject, out var userAccountId))
        {
            return Unauthorized();
        }

        await sender.Send(
            new ChangeMyPasswordCommand(
                userAccountId,
                request.CurrentPassword,
                request.NewPassword),
            cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpPost("revoke")]
    [EndpointSummary("Revoca un Refresh Token y cierra la sesión")]
    [EndpointDescription("Invalida el Refresh Token. Fallo 401: application/problem+json con code fijo Authentication.InvalidRefreshToken.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Revoke(
        [FromBody] RevokeTokenRequest request,
        CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(subject, out var userId))
        {
            return Unauthorized();
        }

        var result = await sender.Send(
            new RevokeTokenCommand(userId, request.RefreshToken),
            cancellationToken);

        if (result.IsFailure)
        {
            // RevokeAsync busca el token solo entre los del usuario autenticado
            // (GetAllByAccountIdAsync(userId)): "no existe" y "es de otro
            // usuario" son indistinguibles y ambos caen en InvalidRefreshToken
            // a propósito, para no filtrar si el token pertenece a alguien más.
            return AuthProblem(StatusCodes.Status401Unauthorized, "Unauthorized", result.Error.Code);
        }

        return NoContent();
    }
}
