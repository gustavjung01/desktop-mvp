using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private const string EmployeeMcpReportingReadPermission = "core.reporting.employee-mcp.read";
    private static readonly HashSet<string> McpRouteSettingsOwnerRoles =
        new(["system:security-owner", "system:implementation-owner"], StringComparer.Ordinal);
    private bool _mcpRoutesSelectionObserverAttached;

    public bool CanViewEmployeeMcpReporting => _access.HasPermission(EmployeeMcpReportingReadPermission);
    public bool IsEmployeeMcpReportingSelected => SelectedWorkspaceIndex == 50;

    public bool CanViewMcpRouteSettings =>
        _access.Current.Roles.Any(McpRouteSettingsOwnerRoles.Contains)
        || _access.HasPermission("mcp.route.write");
    public bool IsMcpRouteSettingsSelected => SelectedWorkspaceIndex == 91;

    internal void InitializeMcpRoutesShell() => EnsureMcpRoutesSelectionObserver();

    public Task NavigateEmployeeMcpReportingAsync()
    {
        EnsureMcpRoutesSelectionObserver();
        SetSelectedNavigation("workforce.employee-mcp-performance");
        IsWorkforceOpen = true;

        if (!CanViewEmployeeMcpReporting)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Hiệu suất nhân viên thị trường.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 50;
        return Task.CompletedTask;
    }

    public Task NavigateMcpRouteSettingsAsync()
    {
        EnsureMcpRoutesSelectionObserver();
        SetSelectedNavigation("settings.mcp-routes");
        IsCompanySettingsOpen = true;

        if (!CanViewMcpRouteSettings)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền thiết lập tuyến MCP.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 91;
        return Task.CompletedTask;
    }

    private void EnsureMcpRoutesSelectionObserver()
    {
        if (_mcpRoutesSelectionObserverAttached) return;

        PropertyChanged += McpRoutesShellPropertyChanged;
        _access.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(CanViewEmployeeMcpReporting));
            OnPropertyChanged(nameof(CanViewMcpRouteSettings));
            if (!CanViewEmployeeMcpReporting && IsEmployeeMcpReportingSelected)
                WorkspaceMessage = "Tài khoản chưa được cấp quyền xem Hiệu suất nhân viên thị trường.";
            if (!CanViewMcpRouteSettings && IsMcpRouteSettingsSelected)
                WorkspaceMessage = "Tài khoản chưa được cấp quyền thiết lập tuyến MCP.";
        };
        _mcpRoutesSelectionObserverAttached = true;
    }

    private void McpRoutesShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(SelectedWorkspaceIndex)) return;
        OnPropertyChanged(nameof(IsEmployeeMcpReportingSelected));
        OnPropertyChanged(nameof(IsMcpRouteSettingsSelected));
    }
}
