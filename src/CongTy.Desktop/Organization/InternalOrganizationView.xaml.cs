using System.Windows;
using System.Windows.Controls;

namespace CongTy.Desktop.Organization;

public partial class InternalOrganizationView : UserControl
{
    private readonly InternalOrganizationViewModel _viewModel;

    public InternalOrganizationView(InternalOrganizationViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    public event Action<string>? NavigationRequested;

    public void SelectNavigationTarget(string target)
    {
        if (string.Equals(target, "overview", StringComparison.Ordinal))
        {
            OrganizationTabs.SelectedIndex = 0;
            return;
        }

        if (string.Equals(target, "branches", StringComparison.Ordinal))
        {
            OrganizationTabs.SelectedIndex = 1;
            return;
        }

        if (string.Equals(target, "warehouses", StringComparison.Ordinal))
        {
            OrganizationTabs.SelectedIndex = 2;
            WarehouseTabs.SelectedIndex = 0;
            return;
        }

        if (string.Equals(target, "locations", StringComparison.Ordinal))
        {
            // Công Ty Web redirects /organization/locations to /organization/warehouses?tab=layout.
            OrganizationTabs.SelectedIndex = 2;
            WarehouseTabs.SelectedIndex = 2;
            _viewModel.EnterWarehouseLayout();
            return;
        }

        OrganizationTabs.SelectedIndex = 0;
    }

    public Task RefreshAsync(CancellationToken cancellationToken = default) =>
        _viewModel.RefreshAsync(cancellationToken);

    private void OverviewBranches_OnClick(object sender, RoutedEventArgs e) =>
        NavigationRequested?.Invoke("branches");

    private void OverviewWarehouses_OnClick(object sender, RoutedEventArgs e) =>
        NavigationRequested?.Invoke("warehouses");

    private void OverviewLocations_OnClick(object sender, RoutedEventArgs e) =>
        NavigationRequested?.Invoke("locations");

    public void OpenCreateBranch() =>
        _viewModel.OpenCreateBranch();

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.RefreshAsync();

    private void OpenBranchRowActions_OnClick(object sender, RoutedEventArgs e) =>
        OpenRowActions(sender, _viewModel.CanWriteBranches);

    private void OpenWarehouseRowActions_OnClick(object sender, RoutedEventArgs e) =>
        OpenRowActions(sender, _viewModel.CanWriteWarehouses);

    private void OpenLocationRowActions_OnClick(object sender, RoutedEventArgs e) =>
        OpenRowActions(sender, _viewModel.CanWriteLocations);

    private void OpenEmployeeRowActions_OnClick(object sender, RoutedEventArgs e) =>
        OpenRowActions(sender, _viewModel.CanWriteEmployees);

    private static void OpenRowActions(object sender, bool canWrite)
    {
        if (sender is not Button button || button.ContextMenu is not ContextMenu menu)
        {
            return;
        }

        menu.DataContext = button.DataContext;
        menu.PlacementTarget = button;

        foreach (var entry in menu.Items)
        {
            if (entry is not MenuItem item)
            {
                continue;
            }

            item.Tag = button.Tag;
            item.IsEnabled = !string.Equals(item.CommandParameter as string, "write", StringComparison.Ordinal) || canWrite;
        }

        menu.IsOpen = true;
    }

    private void AddBranch_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.OpenCreateBranch();

    private void EditBranch_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var id))
        {
            _viewModel.OpenEditBranch(id);
        }
    }

    private void ToggleBranch_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var id))
        {
            _viewModel.OpenBranchStatusConfirm(id);
        }
    }

    private void CancelBranchStatus_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.CancelBranchStatusConfirm();

    private async void ConfirmBranchStatus_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ConfirmBranchStatusAsync();

    private void AddWarehouse_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.OpenCreateWarehouse();

    private void EditWarehouse_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var id))
        {
            _viewModel.OpenEditWarehouse(id);
        }
    }

    private void ToggleWarehouse_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var id))
        {
            _viewModel.OpenWarehouseStatusConfirm(id);
        }
    }

    private void SelectWarehouse_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryGetTag(sender, out var id))
        {
            return;
        }

        _viewModel.SelectedWarehouseId = id;
        WarehouseTabs.SelectedIndex = 2;
        _viewModel.EnterWarehouseLayout();
    }

    private async void WarehouseTabs_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, WarehouseTabs))
        {
            return;
        }

        if (WarehouseTabs.SelectedIndex == 1)
        {
            _viewModel.ResetQuickWarehouseSetup();
        }
        else if (WarehouseTabs.SelectedIndex == 2)
        {
            _viewModel.EnterWarehouseLayout();
        }
        else if (WarehouseTabs.SelectedIndex == 3)
        {
            await _viewModel.LoadLocationModeHistoryAsync();
        }
    }

    private void OpenQuickWarehouse_OnClick(object sender, RoutedEventArgs e)
    {
        _viewModel.ResetQuickWarehouseSetup();
        WarehouseTabs.SelectedIndex = 1;
    }

    private async void SaveQuickWarehouse_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.SaveQuickWarehouseAsync();

    private void AddLocation_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.OpenCreateLocation();

    private void EditLocation_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var id))
        {
            _viewModel.OpenEditLocation(id);
        }
    }

    private void ToggleLocation_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var id))
        {
            _viewModel.OpenLocationStatusConfirm(id);
        }
    }

    private void CancelWarehouseStatus_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.CancelWarehouseStatusConfirm();

    private async void ConfirmWarehouseStatus_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ConfirmWarehouseStatusAsync();

    private void AddEmployee_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.OpenCreateEmployee();

    private void EditEmployee_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var id))
        {
            _viewModel.OpenEditEmployee(id);
        }
    }

    private async void ToggleEmployee_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var id) && ConfirmStatusChange("nhân sự"))
        {
            await _viewModel.ToggleEmployeeAsync(id);
        }
    }

    private async void SaveEditor_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.SaveEditorAsync();

    private void CancelEditor_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.CancelEditor();

    private void OpenLayout_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.OpenLayoutEditor();

    private void CloseLayout_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.CloseLayoutEditor();

    private async void PreviewLayout_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.PreviewLayoutAsync();

    private async void ConfirmLayout_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.ConfirmLayoutAsync();

    private async void LoadLayoutHistory_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.LoadLocationModeHistoryAsync();

    private async void ViewRunDetail_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var id))
        {
            await _viewModel.LoadLocationModeRunDetailAsync(id);
        }
    }

    private static bool TryGetTag(object sender, out string id)
    {
        id = (sender as FrameworkElement)?.Tag as string ?? string.Empty;
        return !string.IsNullOrWhiteSpace(id);
    }

    private static bool ConfirmStatusChange(string entityLabel) =>
        MessageBox.Show(
            $"Xác nhận thay đổi trạng thái {entityLabel}? Dữ liệu lịch sử vẫn được giữ để đối soát.",
            "Xác nhận trạng thái",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question) == MessageBoxResult.Yes;
}
