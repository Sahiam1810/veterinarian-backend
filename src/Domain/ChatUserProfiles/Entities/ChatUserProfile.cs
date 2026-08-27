using Domain.ChatUserProfiles.ValueObjects;
using Domain.Common;

namespace Domain.ChatUserProfiles.Entities;

/// <summary>
/// Perfil de chat asociado a un usuario del sistema (un usuario puede tener varios perfiles).
/// </summary>
public sealed class ChatUserProfile : BaseEntity<Guid>
{
    private ChatUserProfile()
    {
    }

    public Guid PersonId { get; private set; }

    public ProfileDisplayName DisplayName { get; private set; } = null!;

    public ProfileAvatarUrl AvatarUrl { get; private set; } = null!;

    public ProfileBio Bio { get; private set; } = null!;

    /// <summary>
    /// Crea un perfil de chat. Las fechas de auditoría las asigna el dominio.
    /// </summary>
    public static ChatUserProfile Create(
        Guid personId,
        string? displayName,
        string? avatarUrl,
        string? bio)
    {
        EnsurePersonId(personId);

        return new ChatUserProfile
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            DisplayName = ProfileDisplayName.Create(displayName),
            AvatarUrl = ProfileAvatarUrl.Create(avatarUrl),
            Bio = ProfileBio.Create(bio)
        };
    }

    /// <summary>
    /// Actualiza los campos opcionales del perfil.
    /// </summary>
    public void Update(string? displayName, string? avatarUrl, string? bio)
    {
        DisplayName = ProfileDisplayName.Create(displayName);
        AvatarUrl = ProfileAvatarUrl.Create(avatarUrl);
        Bio = ProfileBio.Create(bio);
        Touch();
    }

    private void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    private static void EnsurePersonId(Guid personId)
    {
        if (personId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la persona es obligatorio.",
                nameof(personId));
        }
    }
}
