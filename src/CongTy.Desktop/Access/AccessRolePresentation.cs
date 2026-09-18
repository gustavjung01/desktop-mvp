using System.Globalization;
using System.Text;
using CongTy.Contracts;

namespace CongTy.Desktop.Access;

public static class AccessRolePresentation
{
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");

    private static readonly IReadOnlyDictionary<string, string> ModuleLabels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["organization"] = "Tổ chức và kho hàng",
            ["access"] = "Nhân sự và phân quyền",
            ["customers"] = "Khách hàng",
            ["suppliers"] = "Nhà cung cấp",
            ["products"] = "Sản phẩm",
            ["pricing"] = "Giá bán và khuyến mãi",
            ["inventory"] = "Tồn kho và lô hàng",
            ["document_numbering"] = "Số chứng từ",
            ["sales"] = "Bán hàng",
            ["purchasing"] = "Mua hàng",
            ["accounting"] = "Kế toán",
            ["reporting"] = "Báo cáo"
        };

    public static string ModuleLabel(string? module) =>
        !string.IsNullOrWhiteSpace(module) && ModuleLabels.TryGetValue(module, out var label)
            ? label
            : string.IsNullOrWhiteSpace(module) ? "Nhóm chức năng khác" : module.Trim();

    public static string DateTimeText(string? value)
    {
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            return string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
        return parsed.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Vietnamese);
    }

    public static string NormalizeSearch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character == 'đ' ? 'd' : character);
        }
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    public static string PermissionPreview(
        AccessRoleData role,
        IReadOnlyDictionary<string, string> permissionLabels)
    {
        if (role.PermissionKeys.Length == 0) return "Chưa gán quyền";

        var labels = role.PermissionKeys
            .Take(2)
            .Select(key => permissionLabels.TryGetValue(key, out var label) ? label : key)
            .ToList();
        if (role.PermissionKeys.Length > 2)
            labels.Add($"+{role.PermissionKeys.Length - 2:N0} quyền khác");
        return string.Join(" · ", labels);
    }
}

public sealed record AccessRoleRowView(
    AccessRoleData Source,
    string Code,
    string Name,
    string Description,
    string LoginChallengeText,
    string PermissionCountText,
    string PermissionPreviewText,
    string StatusText,
    bool IsActive,
    string UpdatedAtText);

public sealed class PermissionOptionView : System.ComponentModel.INotifyPropertyChanged
{
    private bool _isSelected;

    public PermissionOptionView(AccessPermissionData source, bool isSelected)
    {
        Source = source;
        _isSelected = isSelected;
    }

    public AccessPermissionData Source { get; }
    public string PermissionKey => Source.PermissionKey;
    public string Label => Source.Label;
    public string Description => Source.Description;
    public bool IsSystem => Source.IsSystem;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}

public sealed record PermissionGroupView(string Module, string Label, IReadOnlyList<PermissionOptionView> Items)
{
    public string CountText => $"{Items.Count:N0} quyền";
}
