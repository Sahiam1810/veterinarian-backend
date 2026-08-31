using Domain.Common;

namespace Domain.RolePermissions.Entities;

public sealed class RolePermission : BaseEntity<Guid>
{
    private RolePermission()
    {
    }

    public RolePermission(
        Guid roleId,
        Guid moduleId,
        bool canView,
        bool canCreate,
        bool canEdit,
        bool canDelete)
    {
        Id = Guid.NewGuid();
        RoleId = roleId;
        ModuleId = moduleId;
        CanView = canView;
        CanCreate = canCreate;
        CanEdit = canEdit;
        CanDelete = canDelete;
    }

    public Guid RoleId { get; private set; }

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
