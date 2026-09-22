using System.Reflection;
using Api.AgentHumans.Controllers;
using Api.ChatConversations.Controllers;
using Api.ChatEscalations.Controllers;
using Api.ChatMessages.Controllers;
using Api.ChatParticipants.Controllers;
using Api.Common.Security;
using Api.Common.Security.Permissions;
using Api.EscalationStatuses.Controllers;
using Api.SenderTypes.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace Api.Tests.Security;

public sealed class ChatPermissionsAuthorizationTests
{
    [Theory]
    // ChatConversationController — módulo "Chat"
    [InlineData(typeof(ChatConversationController), nameof(ChatConversationController.GetAll), "Chat", PermissionAction.View)]
    [InlineData(typeof(ChatConversationController), nameof(ChatConversationController.GetById), "Chat", PermissionAction.View)]
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
    [InlineData(typeof(ChatEscalationController), nameof(ChatEscalationController.Resolve), "Escalamientos", PermissionAction.Edit)]
    // Catálogos del Chat — solo lectura, mismo patrón que PrioritiesController
    [InlineData(typeof(SenderTypesController), nameof(SenderTypesController.GetAll), "Catálogos del Chat", PermissionAction.View)]
    [InlineData(typeof(SenderTypesController), nameof(SenderTypesController.GetById), "Catálogos del Chat", PermissionAction.View)]
    [InlineData(typeof(EscalationStatusesController), nameof(EscalationStatusesController.GetAll), "Catálogos del Chat", PermissionAction.View)]
    [InlineData(typeof(EscalationStatusesController), nameof(EscalationStatusesController.GetById), "Catálogos del Chat", PermissionAction.View)]
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
    // ChatEscalationController: borrar sigue siendo administrativo.
    [InlineData(typeof(ChatEscalationController), nameof(ChatEscalationController.Delete))]
    // Catálogos del Chat: administrar el catálogo en sí (no solo leerlo) sigue siendo administrativo.
    [InlineData(typeof(SenderTypesController), nameof(SenderTypesController.Create))]
    [InlineData(typeof(SenderTypesController), nameof(SenderTypesController.Update))]
    [InlineData(typeof(SenderTypesController), nameof(SenderTypesController.Delete))]
    [InlineData(typeof(EscalationStatusesController), nameof(EscalationStatusesController.Create))]
    [InlineData(typeof(EscalationStatusesController), nameof(EscalationStatusesController.Update))]
    [InlineData(typeof(EscalationStatusesController), nameof(EscalationStatusesController.Delete))]
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
    [InlineData(typeof(SenderTypesController))]
    [InlineData(typeof(EscalationStatusesController))]
    public void Controller_no_longer_declares_a_blanket_class_level_AdminOnly_policy(Type controllerType)
    {
        Assert.Empty(controllerType.GetCustomAttributes<AuthorizeAttribute>(inherit: false));
    }
}
