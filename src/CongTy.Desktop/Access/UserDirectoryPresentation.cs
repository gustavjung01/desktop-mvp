using System.ComponentModel;
using System.Runtime.CompilerServices;
using CongTy.Contracts;

namespace CongTy.Desktop.Access;

public static class UserDirectoryPresentation
{
    public static string RoleText(
        AccessUserData user,
        IReadOnlyDictionary<string, AccessRoleData> roleMap)
    {
        var names = user.RoleIds
            .Select(roleId => roleMap.TryGetValue(roleId, out var role) ? role.Name.Trim() : string.Empty)
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        return names.Length == 0 ? "Chưa gán vai trò" : string.Join(" · ", names);
    }
}

public sealed record UserDirectoryRowView(
    AccessUserData Source,
    string LoginName,
    string EmployeeName,
    string EmployeeCodeText,
    string RoleText,
    string StatusText,
    bool IsActive,
    string UpdatedAtText,
    string ToggleActionText);

public sealed record UserStatusFilterOption(string Key, string Label)
{
    public override string ToString() => Label;
}

public sealed record UserEmployeeOption(string Id, string Label)
{
    public override string ToString() => Label;
}

public sealed record UserDraftStatusOption(bool IsActive, string Label)
{
    public override string ToString() => Label;
}

public sealed class UserRoleOptionView : INotifyPropertyChanged
{
    private bool _isSelected;

    public UserRoleOptionView(string id, string name, bool isActive, bool isSelected)
    {
        Id = id;
        Name = name;
        IsActive = isActive;
        _isSelected = isSelected;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; }
    public string Name { get; }
    public bool IsActive { get; }
    public bool HasNote => !IsActive;
    public string Note => IsActive ? string.Empty : "Vai trò đã ngừng sử dụng — bỏ chọn để thu hồi";

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
