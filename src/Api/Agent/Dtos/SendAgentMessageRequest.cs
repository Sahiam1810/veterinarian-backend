using System.Text.Json.Serialization;

namespace Api.Agent.Dtos;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record SendAgentMessageRequest(
    string Message,
    Guid? ConversationId,
    Guid? PetId,
    string Language);
