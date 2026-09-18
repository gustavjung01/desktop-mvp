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
