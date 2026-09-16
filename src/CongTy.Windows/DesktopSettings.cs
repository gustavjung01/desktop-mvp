namespace CongTy.Windows;

public sealed record DesktopSettings
{
    public string InstallationName { get; init; } = "Cấu hình Công Ty";
    public string CompanyDisplayName { get; init; } = "Công Ty";
    public string ApiBaseUrl { get; init; } = string.Empty;
    public string Theme { get; init; } = "Light";
}
