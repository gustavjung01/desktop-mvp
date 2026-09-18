using System.ComponentModel;
using System.Runtime.CompilerServices;
using CongTy.Contracts;

namespace CongTy.Desktop.Access;

public sealed record UserScopeSelectionState(string[] BranchIds, string[] WarehouseIds);

public static class UserScopeSelectionRules
{
    public static UserScopeSelectionState SetBranch(
        string branchId,
        bool selected,
        IEnumerable<string> currentBranchIds,
        IEnumerable<string> currentWarehouseIds,
        IEnumerable<UserScopeWarehouseData> warehouses)
    {
        var branches = currentBranchIds.ToHashSet(StringComparer.Ordinal);
        var warehouseIds = currentWarehouseIds.ToHashSet(StringComparer.Ordinal);

        if (selected)
        {
            branches.Add(branchId);
        }
        else
        {
            branches.Remove(branchId);
            var branchWarehouses = warehouses
                .Where(warehouse => string.Equals(warehouse.BranchId, branchId, StringComparison.Ordinal))
                .Select(warehouse => warehouse.Id)
                .ToHashSet(StringComparer.Ordinal);
            warehouseIds.RemoveWhere(branchWarehouses.Contains);
        }

        return Sorted(branches, warehouseIds);
    }

    public static UserScopeSelectionState SetWarehouse(
        UserScopeWarehouseData warehouse,
        bool selected,
        IEnumerable<string> currentBranchIds,
        IEnumerable<string> currentWarehouseIds)
    {
        var branches = currentBranchIds.ToHashSet(StringComparer.Ordinal);
        var warehouseIds = currentWarehouseIds.ToHashSet(StringComparer.Ordinal);

        if (selected) warehouseIds.Add(warehouse.Id);
        else warehouseIds.Remove(warehouse.Id);

        branches.Add(warehouse.BranchId);
        return Sorted(branches, warehouseIds);
    }

    private static UserScopeSelectionState Sorted(
        HashSet<string> branches,
        HashSet<string> warehouses) =>
        new(
            branches.OrderBy(id => id, StringComparer.Ordinal).ToArray(),
            warehouses.OrderBy(id => id, StringComparer.Ordinal).ToArray());
}

public sealed class UserScopeUserView : INotifyPropertyChanged
{
    private AccessUserData _source;

    public UserScopeUserView(AccessUserData source) => _source = source;

    public event PropertyChangedEventHandler? PropertyChanged;

    public AccessUserData Source => _source;
    public string Id => _source.Id;
    public string PrimaryText =>
        string.IsNullOrWhiteSpace(_source.EmployeeFullName)
            ? _source.LoginName
            : _source.EmployeeFullName.Trim();
    public string SecondaryText =>
        string.IsNullOrWhiteSpace(_source.EmployeeCode)
            ? _source.LoginName
            : $"{_source.LoginName} · {_source.EmployeeCode.Trim()}";
    public bool IsOwner => !string.IsNullOrWhiteSpace(_source.OwnerKind);
    public string ScopeSummary => IsOwner ? "Toàn Công Ty" : $"{_source.WarehouseIds.Length:N0} kho";

    public void UpdateSource(AccessUserData source)
    {
        _source = source;
        OnPropertyChanged(nameof(Source));
        OnPropertyChanged(nameof(PrimaryText));
        OnPropertyChanged(nameof(SecondaryText));
        OnPropertyChanged(nameof(IsOwner));
        OnPropertyChanged(nameof(ScopeSummary));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class UserScopeBranchOptionView : INotifyPropertyChanged
{
    private bool _isSelected;

    public UserScopeBranchOptionView(UserScopeBranchData source, bool isSelected)
    {
        Source = source;
        _isSelected = isSelected;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public UserScopeBranchData Source { get; }
    public string Id => Source.Id;
    public string Name => Source.Name;
    public string Detail => Source.IsActive
        ? Source.Code
        : $"{Source.Code} · ngừng sử dụng / lịch sử";

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }
}

public sealed class UserScopeWarehouseOptionView : INotifyPropertyChanged
{
    private bool _isSelected;

    public UserScopeWarehouseOptionView(
        UserScopeWarehouseData source,
        string branchName,
        bool isSelected)
    {
        Source = source;
        BranchName = branchName;
        _isSelected = isSelected;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public UserScopeWarehouseData Source { get; }
    public string Id => Source.Id;
    public string BranchId => Source.BranchId;
    public string Name => Source.Name;
    public string BranchName { get; }
    public string Detail => Source.IsActive
        ? $"{Source.Code} · {BranchName}"
        : $"{Source.Code} · {BranchName} · ngừng sử dụng / lịch sử";

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }
}
