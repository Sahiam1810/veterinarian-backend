using Domain.Common;
using Domain.Users.ValueObjects;

namespace Domain.Users.Entities;

public sealed class Users : BaseEntity<Guid>
{
    private Users()
    {
    }

    // USERS es solo personal (Frente 1 separó Clientes en su propia tabla),
    // así que todo usuario aquí se loguea y requiere contraseña.
    public Users(string fullName, string email, string passwordHash, Guid roleId)
    {
        Id = Guid.NewGuid();
        FullName = fullName;
        Email = UserEmail.Create(email);
        PasswordHash = passwordHash;
        PasswordChangedAt = DateTime.UtcNow;
        RoleId = roleId;
        IsActive = true;
    }

    public string FullName { get; private set; } = null!;

    public UserEmail Email { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public DateTime? PasswordChangedAt { get; private set; }

    public Guid RoleId { get; private set; }

    public bool IsActive { get; private set; }

    // EF deja null si PHOTO_URL es NULL; el getter garantiza VO vacío.
    private UserPhotoUrl? _photoUrl;
    public UserPhotoUrl PhotoUrl
    {
        get => _photoUrl ?? UserPhotoUrl.Create(null);
        private set => _photoUrl = value;
    }

    public void Update(string fullName, string email, Guid roleId)
    {
        FullName = fullName;
        Email = UserEmail.Create(email);
        RoleId = roleId;
        UpdatedAt = DateTime.UtcNow;
    }

    // Vacío o nulo quita la foto de perfil.
    public void SetPhotoUrl(string? photoUrl)
    {
        PhotoUrl = UserPhotoUrl.Create(photoUrl);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        PasswordChangedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
