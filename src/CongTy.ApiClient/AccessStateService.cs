using System.Globalization;
using CongTy.Contracts;

namespace CongTy.ApiClient;

public sealed record AccessSnapshot(
    bool IsAuthenticated,
    string? ActorId,
    string? EmployeeId,
    string? LoginName,
    string? EmployeeFullName,
    string? OwnerKind,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    AccessScopesData Scopes,
    DateTimeOffset? SessionExpiresAt)
{
    public static AccessSnapshot Anonymous { get; } = new(
        false,
        null,
        null,
        null,
        null,
        null,
        Array.Empty<string>(),
        Array.Empty<string>(),
        new AccessScopesData(),
        null);

    public bool IsExpired(DateTimeOffset now) =>
        SessionExpiresAt is not null && SessionExpiresAt.Value <= now;
}

public interface IAccessStateService
{
    AccessSnapshot Current { get; }
    event EventHandler<AccessSnapshot>? Changed;
    bool HasPermission(string? permission);
    bool CanNavigate(string? navigationKey);
    bool CanUseAction(string? requiredPermission);
    bool TryApply(InternalMeData data, out string errorMessage);
    void Clear();
}

public sealed class AccessStateService : IAccessStateService
{
    private static readonly HashSet<string> AuthenticatedNavigation =
        new(["home", "access", "system-status", "settings"], StringComparer.Ordinal);

    public AccessSnapshot Current { get; private set; } = AccessSnapshot.Anonymous;

    public event EventHandler<AccessSnapshot>? Changed;

    public bool HasPermission(string? permission) =>
        Current.IsAuthenticated
        && !string.IsNullOrWhiteSpace(permission)
        && Current.Permissions.Contains(permission.Trim(), StringComparer.Ordinal);

    public bool CanNavigate(string? navigationKey)
    {
        if (!Current.IsAuthenticated || string.IsNullOrWhiteSpace(navigationKey))
        {
            return false;
        }

        var key = navigationKey.Trim();
        if (AuthenticatedNavigation.Contains(key))
        {
            return true;
        }

        return key switch
        {
            "internal-organization" => new[]
            {
                "core.organization.read",
                "core.branch.read",
                "core.warehouse.read",
                "core.warehouse.location.read",
                "core.employee.read"
            }.Any(HasPermission),
            "partners" => new[]
            {
                "core.customer.read",
                "core.supplier.read"
            }.Any(HasPermission),
            "sales" => HasPermission("core.sales-order.read"),
            "inventory" => new[]
            {
                "core.inventory.read",
                "core.inventory.lot.read",
                "core.reporting.inventory.read"
            }.Any(HasPermission),
            "fulfillment" => new[]
            {
                "core.fulfillment.read",
                "core.fulfillment.pick"
            }.Any(HasPermission),
            "inventory-transfer" => HasPermission("core.inventory-transfer.read"),
            _ => false
        };
    }

    public bool CanUseAction(string? requiredPermission) =>
        HasPermission(requiredPermission);

    public bool TryApply(InternalMeData data, out string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (string.IsNullOrWhiteSpace(data.ActorId)
            || data.Session is null
            || string.IsNullOrWhiteSpace(data.Session.LoginName)
            || !DateTimeOffset.TryParse(
                data.Session.ExpiresAt,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var expiresAt))
        {
            errorMessage = "Thông tin phiên đăng nhập từ hệ thống không đầy đủ.";
            Clear();
            return false;
        }

        Current = new AccessSnapshot(
            true,
            data.ActorId,
            data.EmployeeId,
            data.Session.LoginName,
            data.Session.EmployeeFullName,
            data.Session.OwnerKind,
            data.Roles.Distinct(StringComparer.Ordinal).ToArray(),
            data.Permissions.Distinct(StringComparer.Ordinal).ToArray(),
            new AccessScopesData
            {
                BranchIds = data.Scopes.BranchIds.Distinct(StringComparer.Ordinal).ToArray(),
                WarehouseIds = data.Scopes.WarehouseIds.Distinct(StringComparer.Ordinal).ToArray(),
                TerritoryIds = data.Scopes.TerritoryIds.Distinct(StringComparer.Ordinal).ToArray()
            },
            expiresAt);

        errorMessage = string.Empty;
        Changed?.Invoke(this, Current);
        return true;
    }

    public void Clear()
    {
        if (ReferenceEquals(Current, AccessSnapshot.Anonymous))
        {
            return;
        }

        Current = AccessSnapshot.Anonymous;
        Changed?.Invoke(this, Current);
    }
}
