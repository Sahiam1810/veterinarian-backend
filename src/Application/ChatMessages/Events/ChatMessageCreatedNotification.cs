using MediatR;
using ChatMessageEntity = Domain.ChatMessages.Entities.ChatMessage;

namespace Application.ChatMessages.Events;

// Publicado después de persistir un ChatMessage. El módulo ChatMessages no
// sabe (ni debe saber) nada de Telegram — es un evento genérico de dominio;
// el reenvío a Telegram del Ticket B4 es solo uno de sus suscriptores,
// implementado del lado Application.Telegram.
public sealed record ChatMessageCreatedNotification(ChatMessageEntity Message) : INotification;
