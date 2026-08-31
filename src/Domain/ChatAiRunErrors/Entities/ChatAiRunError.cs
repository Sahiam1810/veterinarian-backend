using Domain.Common;

namespace Domain.ChatAiRunErrors.Entities;

/// <summary>
/// Error registrado para una ejecución de IA (inmutable tras creación).
/// </summary>
public sealed class ChatAiRunError : BaseEntity<Guid>
{
    public const int ErrorCodeMaxLength = 80;

    public const int ProviderErrorIdMaxLength = 120;

    private ChatAiRunError()
    {
    }

    public Guid ChatAiRunId { get; private set; }

    public string ErrorMessage { get; private set; } = null!;

    public string? ErrorCode { get; private set; }

    public string? ProviderErrorId { get; private set; }

    /// <summary>
    /// Crea un error de ejecución con mensaje obligatorio y campos opcionales.
    /// </summary>
    public static ChatAiRunError Create(
        Guid chatAiRunId,
        string errorMessage,
        string? errorCode = null,
        string? providerErrorId = null)
    {
        EnsureChatAiRunId(chatAiRunId);
        EnsureErrorMessage(errorMessage);
        EnsureErrorCode(errorCode);
        EnsureProviderErrorId(providerErrorId);

        return new ChatAiRunError
        {
            Id = Guid.NewGuid(),
            ChatAiRunId = chatAiRunId,
            ErrorMessage = errorMessage.Trim(),
            ErrorCode = string.IsNullOrWhiteSpace(errorCode) ? null : errorCode.Trim(),
            ProviderErrorId = string.IsNullOrWhiteSpace(providerErrorId) ? null : providerErrorId.Trim()
        };
    }

    private static void EnsureChatAiRunId(Guid chatAiRunId)
    {
        if (chatAiRunId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la ejecución de IA es obligatorio.",
                nameof(chatAiRunId));
        }
    }

    private static void EnsureErrorMessage(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new ArgumentException(
                "El mensaje de error es obligatorio.",
                nameof(errorMessage));
        }
    }

    private static void EnsureErrorCode(string? errorCode)
    {
        if (errorCode is not null && errorCode.Trim().Length > ErrorCodeMaxLength)
        {
            throw new ArgumentException(
                $"El código de error no puede superar los {ErrorCodeMaxLength} caracteres.",
                nameof(errorCode));
        }
    }

    private static void EnsureProviderErrorId(string? providerErrorId)
    {
        if (providerErrorId is not null && providerErrorId.Trim().Length > ProviderErrorIdMaxLength)
        {
            throw new ArgumentException(
                $"El identificador de error del proveedor no puede superar los {ProviderErrorIdMaxLength} caracteres.",
                nameof(providerErrorId));
        }
    }
}
