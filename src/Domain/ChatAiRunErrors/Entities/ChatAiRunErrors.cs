using Domain.ChatAiRunErrors.ValueObjects;
using Domain.Common;

namespace Domain.ChatAiRunErrors.Entities;

// Entidad de errores de una ejecución de chat con IA (tabla chat_ai_run_errors).
public sealed class ChatAiRunErrors : BaseEntity<Guid>
{
    // Constructor privado para EF Core / materialización.
    private ChatAiRunErrors()
    {
    }

    // Crea un error nuevo con Id generado y value objects validados.
    public ChatAiRunErrors(
        Guid aiRunId,
        string? errorMessage = null,
        string? errorCode = null,
        string? providerErrorId = null)
    {
        Id = Guid.NewGuid();
        AiRunId = ChatAiRunId.Create(aiRunId);
        ErrorMessage = ChatAiErrorMessage.Create(errorMessage);
        ErrorCode = ChatAiErrorCode.Create(errorCode);
        ProviderErrorId = ChatAiProviderErrorId.Create(providerErrorId);
    }

    // Identificador de la ejecución de IA (columna ai_run_id).
    public ChatAiRunId AiRunId { get; private set; } = null!;

    // Mensaje o traza del error (columna error_message, CLOB).
    public ChatAiErrorMessage ErrorMessage { get; private set; } = null!;

    // Código de error de negocio o proveedor (columna error_code).
    public ChatAiErrorCode ErrorCode { get; private set; } = null!;

    // Id del error en el proveedor externo (columna provider_error_id).
    public ChatAiProviderErrorId ProviderErrorId { get; private set; } = null!;

    // TODO: habilitar cuando exista la entidad de ai_runs
    // public AiRun AiRun { get; private set; } = null!;

    // Actualiza el detalle del error y marca UpdatedAt en UTC.
    public void Update(
        string? errorMessage,
        string? errorCode,
        string? providerErrorId)
    {
        ErrorMessage = ChatAiErrorMessage.Create(errorMessage);
        ErrorCode = ChatAiErrorCode.Create(errorCode);
        ProviderErrorId = ChatAiProviderErrorId.Create(providerErrorId);
        UpdatedAt = DateTime.UtcNow;
    }
}
