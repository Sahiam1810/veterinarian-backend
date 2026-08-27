using Domain.Common;

namespace Domain.AgentHumans.Entities;

/// <summary>
/// Agente humano de chat asociado a un usuario del sistema (un usuario puede tener varios agentes).
/// </summary>
public sealed class AgentHuman : BaseEntity<Guid>
{
    private AgentHuman()
    {
    }

    public Guid UserId { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>
    /// Crea un agente humano activo. Las fechas de auditoría las asigna el dominio.
    /// </summary>
    public static AgentHuman Create(Guid userId)
    {
        EnsureUserId(userId);

        return new AgentHuman
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsActive = true
        };
    }

    /// <summary>
    /// Marca el agente como activo.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    /// <summary>
    /// Marca el agente como inactivo.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    private static void EnsureUserId(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del usuario es obligatorio.",
                nameof(userId));
        }
    }
}
