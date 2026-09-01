using Api.Common.Security;
using Api.Common.Security.Permissions;
using Api.Notifications.Dtos;
using Api.Notifications.Mappings;
using Application.Notifications.UseCases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Notifications.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class NotificationsController(ISender sender) : ControllerBase
{
    // La matriz de permisos no le da Crear a ningún rol sobre este módulo a
    // propósito: las notificaciones las genera el sistema (ver
    // GenerateUpcomingAppointmentRemindersCommandHandler), no un humano por API.
    [HttpPost]
    [RequirePermission("Notificaciones", PermissionAction.Create)]
    [EndpointSummary("Crea una nueva notificación")]
    [EndpointDescription("Registra una nueva notificación asociada a un usuario y una cita médica.")]
    [ProducesResponseType(typeof(CreateNotificationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateNotificationResponse>> Create(
        [FromBody] CreateNotificationRequest request,
        CancellationToken cancellationToken)
    {
        var id = await sender.Send(
            request.ToCommand(),
            cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new CreateNotificationResponse(id));
    }

    [HttpGet]
    [RequirePermission("Notificaciones", PermissionAction.View)]
    [EndpointSummary("Obtiene todas las notificaciones")]
    [EndpointDescription("Retorna el listado completo de todas las notificaciones registradas.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<NotificationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<NotificationResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var notifications = await sender.Send(
            new GetAllNotificationsQuery(),
            cancellationToken);

        return Ok(notifications.ToResponse());
    }

    [HttpGet("{id:guid}")]
    [RequirePermission("Notificaciones", PermissionAction.View)]
    [EndpointSummary("Obtiene una notificación por su ID")]
    [EndpointDescription("Retorna la información detallada de una notificación específica.")]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var notification = await sender.Send(
            new GetNotificationByIdQuery(id),
            cancellationToken);

        return notification is null
            ? NotFound()
            : Ok(notification.ToResponse());
    }

    [HttpGet("user/{userId:guid}")]
    [RequirePermission("Notificaciones", PermissionAction.View)]
    [EndpointSummary("Obtiene las notificaciones de un usuario")]
    [EndpointDescription("Retorna todas las notificaciones pertenecientes a un usuario especificado.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<NotificationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<NotificationResponse>>> GetByUserId(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var notifications = await sender.Send(
            new GetNotificationsByUserIdQuery(userId),
            cancellationToken);

        return Ok(notifications.ToResponse());
    }

    [HttpGet("appointment/{appointmentId:guid}")]
    [RequirePermission("Notificaciones", PermissionAction.View)]
    [EndpointSummary("Obtiene las notificaciones de una cita")]
    [EndpointDescription("Retorna todas las notificaciones asociadas a una cita médica especificada.")]
    [ProducesResponseType(typeof(IReadOnlyCollection<NotificationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<NotificationResponse>>> GetByAppointmentId(
        Guid appointmentId,
        CancellationToken cancellationToken)
    {
        var notifications = await sender.Send(
            new GetNotificationsByAppointmentIdQuery(appointmentId),
            cancellationToken);

        return Ok(notifications.ToResponse());
    }

    [HttpPut("{id:guid}")]
    [RequirePermission("Notificaciones", PermissionAction.Edit)]
    [EndpointSummary("Actualiza una notificación existente")]
    [EndpointDescription("Modifica los datos de una notificación previamente registrada.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateNotificationRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await sender.Send(
            request.ToCommand(id),
            cancellationToken);

        return updated
            ? NoContent()
            : NotFound();
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission("Notificaciones", PermissionAction.Delete)]
    [EndpointSummary("Elimina una notificación por su ID")]
    [EndpointDescription("Remueve permanentemente una notificación del sistema.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await sender.Send(
            new DeleteNotificationCommand(id),
            cancellationToken);

        return deleted
            ? NoContent()
            : NotFound();
    }
}
