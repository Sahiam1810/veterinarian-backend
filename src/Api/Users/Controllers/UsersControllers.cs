using Api.Common.Security;
using Api.Common.Security.Permissions;
using Api.Users.Dtos;
using Api.Users.Mappings;
using Application.Users.UseCase;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Users.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UsersController(ISender sender) : ControllerBase
{
    [HttpPost]
    [RequirePermission("Usuarios", PermissionAction.Create)]
    [EndpointSummary("Crea un nuevo usuario")]
    [EndpointDescription("Registra un nuevo usuario del sistema, asignándole un rol existente. " +
        "IMPORTANTE: esto por sí solo NO deja al usuario en condiciones de iniciar sesión — la contraseña " +
        "que se envía aquí no se usa para autenticar. Para tener un usuario funcional hay que completar, " +
        "en este orden: (1) POST /api/users (este endpoint), (2) POST /api/useraccounts con el UserId " +
        "devuelto, y (3) POST /api/usercredentials con el AccountId devuelto — recién ahí el usuario puede " +
        "loguearse en POST /api/auth/login.")]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateUserResponse>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var userId = await sender.Send(
            request.ToCommand(),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new CreateUserResponse(userId));
    }

    [HttpGet]
    [RequirePermission("Usuarios", PermissionAction.View)]
    [EndpointSummary("Obtiene todos los usuarios")]
    [EndpointDescription("Retorna el listado de todos los usuarios registrados, con su rol y estado activo/inactivo.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<UserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<UserResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var users = await sender.Send(
            new GetAllUsersQuery(),
            cancellationToken);

        return Ok(users.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("Usuarios", PermissionAction.View)]
    [EndpointSummary("Obtiene un usuario por su ID")]
    [EndpointDescription("Retorna la información de un usuario específico por su identificador GUID.")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await sender.Send(
            new GetUserByIdQuery(id),
            cancellationToken);

        return Ok(user.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Usuarios", PermissionAction.Edit)]
    [EndpointSummary("Actualiza los datos de un usuario")]
    [EndpointDescription("Modifica el nombre completo, correo o rol de un usuario existente.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            request.ToCommand(id),
            cancellationToken);

        return NoContent();
    }

    [HttpPatch("{id:guid}/deactivate")]
    [RequirePermission("Usuarios", PermissionAction.Edit)]
    [EndpointSummary("Desactiva un usuario")]
    [EndpointDescription("Marca a un usuario como inactivo, revocando su acceso al sistema sin eliminarlo.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new DeactivateUserCommand(id),
            cancellationToken);

        return NoContent();
    }

    [HttpPatch("{id:guid}/activate")]
    [RequirePermission("Usuarios", PermissionAction.Edit)]
    [EndpointSummary("Activa un usuario")]
    [EndpointDescription("Restaura el acceso de un usuario previamente desactivado.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(
        Guid id,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new ActivateUserCommand(id),
            cancellationToken);

        return NoContent();
    }
}
