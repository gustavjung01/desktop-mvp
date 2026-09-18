using System.ComponentModel;

namespace CongTy.Desktop.Shell;

public sealed partial class ShellViewModel
{
    private const string McpRoutesReadPermission = "core.reporting.employee-mcp.read";
    private bool _mcpRoutesSelectionObserverAttached;

    public bool CanViewMcpRoutes => _access.HasPermission(McpRoutesReadPermission);
    public bool IsMcpRoutesSelected => SelectedWorkspaceIndex == 50;

    internal void InitializeMcpRoutesShell() => EnsureMcpRoutesSelectionObserver();

    public Task NavigateMcpRoutesAsync()
    {
        EnsureMcpRoutesSelectionObserver();
        SetSelectedNavigation("settings.mcp-routes");
        IsCompanySettingsOpen = true;

        if (!CanViewMcpRoutes)
        {
            WorkspaceMessage = "Tài khoản chưa được cấp quyền xem MCP và tuyến.";
            return Task.CompletedTask;
        }

        WorkspaceMessage = string.Empty;
        SelectedWorkspaceIndex = 50;
        return Task.CompletedTask;
    }

    private void EnsureMcpRoutesSelectionObserver()
    {
        if (_mcpRoutesSelectionObserverAttached) return;

        PropertyChanged += McpRoutesShellPropertyChanged;
        _access.Changed += (_, _) =>
        {
            OnPropertyChanged(nameof(CanViewMcpRoutes));
            if (!CanViewMcpRoutes && IsMcpRoutesSelected)
                WorkspaceMessage = "Tài khoản chưa được cấp quyền xem MCP và tuyến.";
        };
        _mcpRoutesSelectionObserverAttached = true;
    }

    private void McpRoutesShellPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SelectedWorkspaceIndex))
            OnPropertyChanged(nameof(IsMcpRoutesSelected));
    }
}
