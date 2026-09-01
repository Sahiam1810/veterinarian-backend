using Domain.Common;

namespace Domain.UserPermissions.Entities;

// Permiso puntual: se suma al del rol del usuario, nunca lo reemplaza.
public sealed class UserPermission : BaseEntity<Guid>
{
    private UserPermission()
    {
    }

    public UserPermission(
        Guid userId,
        Guid moduleId,
        bool canView,
        bool canCreate,
        bool canEdit,
        bool canDelete)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        ModuleId = moduleId;
        CanView = canView;
        CanCreate = canCreate;
        CanEdit = canEdit;
        CanDelete = canDelete;
    }

    public Guid UserId { get; private set; }

    public Guid ModuleId { get; private set; }

    public bool CanView { get; private set; }

    public bool CanCreate { get; private set; }

    public bool CanEdit { get; private set; }

    public bool CanDelete { get; private set; }

    public void UpdatePermissions(
        bool canView,
        bool canCreate,
        bool canEdit,
        bool canDelete)
    {
        CanView = canView;
        CanCreate = canCreate;
        CanEdit = canEdit;
        CanDelete = canDelete;
        UpdatedAt = DateTime.UtcNow;
    }
}
