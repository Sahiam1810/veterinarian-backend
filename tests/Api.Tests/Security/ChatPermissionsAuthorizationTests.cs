using System.Reflection;
using Api.AgentHumans.Controllers;
using Api.ChatConversationAssignments.Controllers;
using Api.ChatConversations.Controllers;
using Api.ChatEscalationAssignments.Controllers;
using Api.ChatEscalationResolutions.Controllers;
using Api.ChatEscalations.Controllers;
using Api.ChatEscalationStatusHistories.Controllers;
using Api.ChatMessages.Controllers;
using Api.ChatParticipants.Controllers;
using Api.Common.Security;
using Api.Common.Security.Permissions;
using Api.ConversationStatuses.Controllers;
using Api.EscalationStatuses.Controllers;
using Api.MessageTypes.Controllers;
using Api.SenderTypes.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Api.Tests.Security;

// Ticket B1: los controladores de chat dejaron de ser AdminOnly en bloque —
// ahora cada acción declara su propio permiso granular (o se queda en
// AdminOnly explícito por método, mismo patrón que PrioritiesController).
// Ningún controlador de esta lista conserva un [Authorize] a nivel de clase.
public sealed class ChatPermissionsAuthorizationTests
{
    [Theory]
    // ChatConversationController — módulo "Chat"
    [InlineData(typeof(ChatConversationController), nameof(ChatConversationController.GetAll), "Chat", PermissionAction.View)]
    [InlineData(typeof(ChatConversationController), nameof(ChatConversationController.GetById), "Chat", PermissionAction.View)]
    [InlineData(typeof(ChatConversationController), nameof(ChatConversationController.UpdateStatus), "Chat", PermissionAction.Edit)]
    [InlineData(typeof(ChatConversationController), nameof(ChatConversationController.UpdatePriority), "Chat", PermissionAction.Edit)]
    [InlineData(typeof(ChatConversationController), nameof(ChatConversationController.UpdateAiEnabled), "Chat", PermissionAction.Edit)]
    [InlineData(typeof(ChatConversationController), nameof(ChatConversationController.Close), "Chat", PermissionAction.Edit)]
    [InlineData(typeof(ChatConversationController), nameof(ChatConversationController.Reopen), "Chat", PermissionAction.Edit)]
    // ChatMessageController — módulo "Chat"
    [InlineData(typeof(ChatMessageController), nameof(ChatMessageController.Create), "Chat", PermissionAction.Create)]
    [InlineData(typeof(ChatMessageController), nameof(ChatMessageController.GetById), "Chat", PermissionAction.View)]
    [InlineData(typeof(ChatMessageController), nameof(ChatMessageController.GetByConversationId), "Chat", PermissionAction.View)]
    // ChatParticipantController — módulo "Chat"
    [InlineData(typeof(ChatParticipantController), nameof(ChatParticipantController.Create), "Chat", PermissionAction.Create)]
    [InlineData(typeof(ChatParticipantController), nameof(ChatParticipantController.GetById), "Chat", PermissionAction.View)]
    [InlineData(typeof(ChatParticipantController), nameof(ChatParticipantController.GetByConversationId), "Chat", PermissionAction.View)]
    [InlineData(typeof(ChatParticipantController), nameof(ChatParticipantController.ChangeIdentity), "Chat", PermissionAction.Edit)]
    // AgentHumanController — módulo "Chat"
    [InlineData(typeof(AgentHumanController), nameof(AgentHumanController.Create), "Chat", PermissionAction.Create)]
    [InlineData(typeof(AgentHumanController), nameof(AgentHumanController.GetAll), "Chat", PermissionAction.View)]
    [InlineData(typeof(AgentHumanController), nameof(AgentHumanController.GetById), "Chat", PermissionAction.View)]
    [InlineData(typeof(AgentHumanController), nameof(AgentHumanController.GetByUserId), "Chat", PermissionAction.View)]
    // ChatEscalationController — módulo "Escalamientos"
    [InlineData(typeof(ChatEscalationController), nameof(ChatEscalationController.Create), "Escalamientos", PermissionAction.Create)]
    [InlineData(typeof(ChatEscalationController), nameof(ChatEscalationController.GetAll), "Escalamientos", PermissionAction.View)]
    [InlineData(typeof(ChatEscalationController), nameof(ChatEscalationController.GetById), "Escalamientos", PermissionAction.View)]
    [InlineData(typeof(ChatEscalationController), nameof(ChatEscalationController.GetByConversationId), "Escalamientos", PermissionAction.View)]
    [InlineData(typeof(ChatEscalationController), nameof(ChatEscalationController.Update), "Escalamientos", PermissionAction.Edit)]
    // ChatEscalationResolutionController — módulo "Escalamientos"
    [InlineData(typeof(ChatEscalationResolutionController), nameof(ChatEscalationResolutionController.Create), "Escalamientos", PermissionAction.Create)]
    [InlineData(typeof(ChatEscalationResolutionController), nameof(ChatEscalationResolutionController.GetAll), "Escalamientos", PermissionAction.View)]
    [InlineData(typeof(ChatEscalationResolutionController), nameof(ChatEscalationResolutionController.GetById), "Escalamientos", PermissionAction.View)]
    [InlineData(typeof(ChatEscalationResolutionController), nameof(ChatEscalationResolutionController.GetByChatEscalationId), "Escalamientos", PermissionAction.View)]
    [InlineData(typeof(ChatEscalationResolutionController), nameof(ChatEscalationResolutionController.Update), "Escalamientos", PermissionAction.Edit)]
    // ChatEscalationAssignmentController — solo lectura para Recepcionista
    [InlineData(typeof(ChatEscalationAssignmentController), nameof(ChatEscalationAssignmentController.GetAll), "Escalamientos", PermissionAction.View)]
    [InlineData(typeof(ChatEscalationAssignmentController), nameof(ChatEscalationAssignmentController.GetById), "Escalamientos", PermissionAction.View)]
    [InlineData(typeof(ChatEscalationAssignmentController), nameof(ChatEscalationAssignmentController.GetByChatEscalationId), "Escalamientos", PermissionAction.View)]
    [InlineData(typeof(ChatEscalationAssignmentController), nameof(ChatEscalationAssignmentController.GetByAgentHumanId), "Escalamientos", PermissionAction.View)]
    // ChatConversationAssignmentController — solo lectura para Recepcionista
    [InlineData(typeof(ChatConversationAssignmentController), nameof(ChatConversationAssignmentController.GetAll), "Escalamientos", PermissionAction.View)]
    [InlineData(typeof(ChatConversationAssignmentController), nameof(ChatConversationAssignmentController.GetByConversationId), "Escalamientos", PermissionAction.View)]
    [InlineData(typeof(ChatConversationAssignmentController), nameof(ChatConversationAssignmentController.GetByAgentHumanId), "Escalamientos", PermissionAction.View)]
    // ChatEscalationStatusHistoryController — solo lectura para Recepcionista
    [InlineData(typeof(ChatEscalationStatusHistoryController), nameof(ChatEscalationStatusHistoryController.GetAll), "Escalamientos", PermissionAction.View)]
    [InlineData(typeof(ChatEscalationStatusHistoryController), nameof(ChatEscalationStatusHistoryController.GetById), "Escalamientos", PermissionAction.View)]
    [InlineData(typeof(ChatEscalationStatusHistoryController), nameof(ChatEscalationStatusHistoryController.GetByChatEscalationId), "Escalamientos", PermissionAction.View)]
    // Catálogos del Chat — solo lectura, mismo patrón que PrioritiesController
    [InlineData(typeof(ConversationStatusesController), nameof(ConversationStatusesController.GetAll), "Catálogos del Chat", PermissionAction.View)]
    [InlineData(typeof(ConversationStatusesController), nameof(ConversationStatusesController.GetById), "Catálogos del Chat", PermissionAction.View)]
    [InlineData(typeof(SenderTypesController), nameof(SenderTypesController.GetAll), "Catálogos del Chat", PermissionAction.View)]
    [InlineData(typeof(SenderTypesController), nameof(SenderTypesController.GetById), "Catálogos del Chat", PermissionAction.View)]
    [InlineData(typeof(EscalationStatusesController), nameof(EscalationStatusesController.GetAll), "Catálogos del Chat", PermissionAction.View)]
    [InlineData(typeof(EscalationStatusesController), nameof(EscalationStatusesController.GetById), "Catálogos del Chat", PermissionAction.View)]
    [InlineData(typeof(MessageTypesController), nameof(MessageTypesController.GetAll), "Catálogos del Chat", PermissionAction.View)]
    [InlineData(typeof(MessageTypesController), nameof(MessageTypesController.GetById), "Catálogos del Chat", PermissionAction.View)]
    public void Action_requires_the_expected_granular_permission(
        Type controllerType,
        string methodName,
        string expectedModule,
        PermissionAction expectedAction)
    {
        var method = controllerType.GetMethod(methodName);
        Assert.NotNull(method);

        var requirePermission = method!.GetCustomAttribute<RequirePermissionAttribute>();
        Assert.NotNull(requirePermission);
        Assert.Equal(
            $"{RequirePermissionAttribute.PolicyPrefix}{expectedModule}:{expectedAction}",
            requirePermission!.Policy);
        // RequirePermissionAttribute ya hereda de AuthorizeAttribute — confirma que no
        // hay un segundo [Authorize]/[RequirePermission] adicional pisando la policy.
        Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>(inherit: false));
    }

    [Theory]
    // ChatConversationController: crear una conversación a mano sigue siendo administrativo.
    [InlineData(typeof(ChatConversationController), nameof(ChatConversationController.Create))]
    // AgentHumanController: el ciclo de vida del "carnet" de agente (editar/activar/desactivar)
    // sigue siendo administrativo — Recepcionista solo puede crear el suyo propio y verlo.
    [InlineData(typeof(AgentHumanController), nameof(AgentHumanController.Update))]
    [InlineData(typeof(AgentHumanController), nameof(AgentHumanController.Activate))]
    [InlineData(typeof(AgentHumanController), nameof(AgentHumanController.Deactivate))]
    // ChatEscalationController / ChatEscalationResolutionController: borrar sigue siendo administrativo.
    [InlineData(typeof(ChatEscalationController), nameof(ChatEscalationController.Delete))]
    [InlineData(typeof(ChatEscalationResolutionController), nameof(ChatEscalationResolutionController.Delete))]
    // ChatEscalationAssignmentController / ChatConversationAssignmentController: la asignación
    // formal a un agente humano queda fuera de esta ronda (ver Ticket B1, "Fuera de alcance") —
    // sigue siendo administrativa en su totalidad salvo la lectura ya cubierta arriba.
    [InlineData(typeof(ChatEscalationAssignmentController), nameof(ChatEscalationAssignmentController.Create))]
    [InlineData(typeof(ChatEscalationAssignmentController), nameof(ChatEscalationAssignmentController.Update))]
    [InlineData(typeof(ChatEscalationAssignmentController), nameof(ChatEscalationAssignmentController.Delete))]
    [InlineData(typeof(ChatConversationAssignmentController), nameof(ChatConversationAssignmentController.Create))]
    [InlineData(typeof(ChatConversationAssignmentController), nameof(ChatConversationAssignmentController.Update))]
    [InlineData(typeof(ChatConversationAssignmentController), nameof(ChatConversationAssignmentController.Delete))]
    // ChatEscalationStatusHistoryController: historial de auditoría, solo lectura para Recepcionista.
    [InlineData(typeof(ChatEscalationStatusHistoryController), nameof(ChatEscalationStatusHistoryController.Create))]
    [InlineData(typeof(ChatEscalationStatusHistoryController), nameof(ChatEscalationStatusHistoryController.Update))]
    [InlineData(typeof(ChatEscalationStatusHistoryController), nameof(ChatEscalationStatusHistoryController.Delete))]
    // Catálogos del Chat: administrar el catálogo en sí (no solo leerlo) sigue siendo administrativo.
    [InlineData(typeof(ConversationStatusesController), nameof(ConversationStatusesController.Create))]
    [InlineData(typeof(ConversationStatusesController), nameof(ConversationStatusesController.Update))]
    [InlineData(typeof(ConversationStatusesController), nameof(ConversationStatusesController.Delete))]
    [InlineData(typeof(SenderTypesController), nameof(SenderTypesController.Create))]
    [InlineData(typeof(SenderTypesController), nameof(SenderTypesController.Update))]
    [InlineData(typeof(SenderTypesController), nameof(SenderTypesController.Delete))]
    [InlineData(typeof(EscalationStatusesController), nameof(EscalationStatusesController.Create))]
    [InlineData(typeof(EscalationStatusesController), nameof(EscalationStatusesController.Update))]
    [InlineData(typeof(EscalationStatusesController), nameof(EscalationStatusesController.Delete))]
    [InlineData(typeof(MessageTypesController), nameof(MessageTypesController.Create))]
    [InlineData(typeof(MessageTypesController), nameof(MessageTypesController.Update))]
    [InlineData(typeof(MessageTypesController), nameof(MessageTypesController.Delete))]
    public void Action_stays_AdminOnly_explicitly_at_the_method_level(Type controllerType, string methodName)
    {
        var method = controllerType.GetMethod(methodName);
        Assert.NotNull(method);

        var authorizeAttr = method!.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttr);
        Assert.Equal(AuthorizationPolicies.AdminOnly, authorizeAttr!.Policy);
        Assert.Null(method.GetCustomAttribute<RequirePermissionAttribute>());
    }

    [Theory]
    [InlineData(typeof(ChatConversationController))]
    [InlineData(typeof(ChatMessageController))]
    [InlineData(typeof(ChatParticipantController))]
    [InlineData(typeof(AgentHumanController))]
    [InlineData(typeof(ChatEscalationController))]
    [InlineData(typeof(ChatEscalationResolutionController))]
    [InlineData(typeof(ChatEscalationAssignmentController))]
    [InlineData(typeof(ChatConversationAssignmentController))]
    [InlineData(typeof(ChatEscalationStatusHistoryController))]
    [InlineData(typeof(ConversationStatusesController))]
    [InlineData(typeof(SenderTypesController))]
    [InlineData(typeof(MessageTypesController))]
    [InlineData(typeof(EscalationStatusesController))]
    public void Controller_no_longer_declares_a_blanket_class_level_AdminOnly_policy(Type controllerType)
    {
        // Cada acción declara su propia autorización ahora (RequirePermission o
        // AdminOnly explícito) — una policy a nivel de clase volvería a bloquear
        // en bloque a Recepcionista, deshaciendo este ticket en silencio.
        Assert.Empty(controllerType.GetCustomAttributes<AuthorizeAttribute>(inherit: false));
    }
}
