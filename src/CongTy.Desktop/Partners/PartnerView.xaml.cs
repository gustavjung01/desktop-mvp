using System.Diagnostics;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace CongTy.Desktop.Partners;

public partial class PartnerView : UserControl
{
    private readonly PartnerViewModel _viewModel;

    public PartnerView(PartnerViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    public void SelectNavigationTarget(string target)
    {
        PartnerTabs.SelectedIndex = string.Equals(target, "suppliers", StringComparison.Ordinal) ? 1 : 0;
        if (PartnerTabs.SelectedIndex == 0)
        {
            CustomerTabs.SelectedIndex = 0;
            _viewModel.SetCustomerWorkspaceTab(0);
        }
    }

    public Task RefreshAsync(CancellationToken cancellationToken = default) =>
        _viewModel.RefreshAsync(cancellationToken);

    public void OpenCustomerTopbarCreate()
    {
        if (_viewModel.IsCustomerGroupSection)
        {
            _viewModel.OpenCreateGroup();
            return;
        }

        _viewModel.OpenCreateCustomer();
    }

    public void ReturnToCustomerList()
    {
        CustomerTabs.SelectedIndex = 0;
        _viewModel.CloseCustomerProfile();
    }

    public void EditSelectedCustomer()
    {
        if (!string.IsNullOrWhiteSpace(_viewModel.SelectedCustomerId))
        {
            _viewModel.OpenEditCustomer(_viewModel.SelectedCustomerId);
        }
    }

    private async void Refresh_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.RefreshAsync();

    private void AddCustomer_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.OpenCreateCustomer();

    private void EditCustomer_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var id))
        {
            _viewModel.OpenEditCustomer(id);
        }
    }

    private async void ToggleCustomer_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var id))
        {
            await _viewModel.ToggleCustomerAsync(id);
        }
    }

    private async void CustomerName_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryGetTag(sender, out var id))
        {
            return;
        }

        await _viewModel.OpenCustomerOverviewAsync(id);
        CustomerTabs.SelectedIndex = 5;
        CustomerProfileTabs.SelectedIndex = 0;
    }

    private async void CustomerOverview_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryGetTag(sender, out var id))
        {
            return;
        }

        await _viewModel.OpenCustomerOverviewAsync(id);
        CustomerTabs.SelectedIndex = 5;
        CustomerProfileTabs.SelectedIndex = 0;
    }

    private void OpenCustomerRowActions_OnClick(object sender, RoutedEventArgs e) =>
        OpenRowActions(sender, _viewModel.CanWriteCustomers);

    private void OpenCustomerGroupRowActions_OnClick(object sender, RoutedEventArgs e) =>
        OpenRowActions(sender, _viewModel.CanWriteCustomers);

    private void OpenSupplierRowActions_OnClick(object sender, RoutedEventArgs e) =>
        OpenRowActions(sender, _viewModel.CanWriteSuppliers);

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

    private async void CustomerAddresses_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryGetTag(sender, out var id))
        {
            return;
        }

        await _viewModel.OpenCustomerAddressesAsync(id);
    }

    private void CloseCustomerAddressManager_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.CloseCustomerAddressManager();

    private async void CustomerTabs_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, CustomerTabs))
        {
            return;
        }

        _viewModel.SetCustomerWorkspaceTab(CustomerTabs.SelectedIndex);

        if (CustomerTabs.SelectedIndex == 2)
        {
            _viewModel.BulkMode = "import";
        }
        else if (CustomerTabs.SelectedIndex == 3)
        {
            _viewModel.BulkMode = "update";
            if (_viewModel.HasBulkFile)
            {
                await _viewModel.IdentifyBulkCustomersAsync();
            }
        }
        else if (CustomerTabs.SelectedIndex == 1
                 && QuickCustomerList.SelectedIndex < 0
                 && QuickCustomerList.Items.Count > 0)
        {
            QuickCustomerList.SelectedIndex = 0;
        }
    }

    private async void QuickCustomerList_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (QuickCustomerList.SelectedValue is not string id || string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        await _viewModel.OpenCustomerOverviewAsync(id);
        await _viewModel.LoadCustomerProfileSectionAsync("info");
    }

    private async void BulkHeader_OnClick(object sender, RoutedEventArgs e)
    {
        if (_viewModel.BulkMode == "update" && _viewModel.HasBulkFile)
        {
            await _viewModel.IdentifyBulkCustomersAsync();
        }
    }

    private void OpenCustomerAddressLocation_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: CustomerAddressRow row }
            || !Uri.TryCreate(row.Location, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps)
        {
            MessageBox.Show(
                "Địa chỉ này chưa có link định vị HTTPS hợp lệ.",
                "Công Ty Desktop",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
    }

    private void PreviewCustomerMedia_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: CustomerMediaRow row } || row.Image is null)
        {
            return;
        }

        var preview = new Window
        {
            Title = $"Ảnh khách hàng · {row.Source}",
            Width = 920,
            Height = 720,
            MinWidth = 640,
            MinHeight = 480,
            Owner = Window.GetWindow(this),
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = FindResource("WindowBackgroundBrush") as System.Windows.Media.Brush
        };

        preview.Content = new Grid
        {
            Margin = new Thickness(18),
            Children =
            {
                new Image
                {
                    Source = row.Image,
                    Stretch = Stretch.Uniform
                }
            }
        };
        preview.ShowDialog();
    }

    private void CustomerList_OnClick(object sender, RoutedEventArgs e) =>
        ReturnToCustomerList();

    private async void CustomerPeriod_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string period })
        {
            return;
        }

        _viewModel.CustomerOverviewPeriod = period;
        await _viewModel.ReloadCustomerOverviewAsync();
    }

    private async void ReloadCustomerOverview_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.ReloadCustomerOverviewAsync();
        var section = SelectedCustomerProfileSection();
        if (section != "overview")
        {
            await _viewModel.LoadCustomerProfileSectionAsync(section);
        }
    }

    private string SelectedCustomerProfileSection() =>
        CustomerProfileTabs.SelectedIndex switch
        {
            0 => "overview",
            1 => "purchased-items",
            2 => "orders",
            3 => "finance",
            4 => "delivery-returns",
            5 => "info",
            _ => "overview"
        };

    private async void CustomerProfileTabs_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, CustomerProfileTabs))
        {
            return;
        }

        await _viewModel.LoadCustomerProfileSectionAsync(SelectedCustomerProfileSection());
    }

    private async void SearchCustomerPurchased_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.SearchCustomerPurchasedItemsAsync();

    private async void PreviousCustomerPurchased_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.PreviousCustomerPurchasedItemsAsync();

    private async void NextCustomerPurchased_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NextCustomerPurchasedItemsAsync();

    private async void SearchCustomerOrders_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.SearchCustomerOrdersAsync();

    private async void PreviousCustomerOrders_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.PreviousCustomerOrdersAsync();

    private async void NextCustomerOrders_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NextCustomerOrdersAsync();

    private async void PreviousCustomerReceivables_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.PreviousCustomerReceivablesAsync();

    private async void NextCustomerReceivables_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NextCustomerReceivablesAsync();

    private async void PreviousCustomerPayments_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.PreviousCustomerPaymentsAsync();

    private async void NextCustomerPayments_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NextCustomerPaymentsAsync();

    private async void PreviousCustomerDeliveries_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.PreviousCustomerDeliveriesAsync();

    private async void NextCustomerDeliveries_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NextCustomerDeliveriesAsync();

    private async void PreviousCustomerReturns_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.PreviousCustomerReturnsAsync();

    private async void NextCustomerReturns_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.NextCustomerReturnsAsync();

    private async void AddCustomerMedia_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn ảnh khách hàng",
            Filter = "Ảnh JPEG, PNG hoặc WebP (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp",
            CheckFileExists = true,
            Multiselect = true
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) != true)
        {
            return;
        }

        foreach (var fileName in dialog.FileNames)
        {
            await _viewModel.UploadCustomerMediaAsync(fileName);
            if (!_viewModel.CanAddCustomerMedia)
            {
                break;
            }
        }
    }

    private void AddGroup_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.OpenCreateGroup();

    private void EditGroup_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var id))
        {
            _viewModel.OpenEditGroup(id);
        }
    }

    private async void ToggleGroup_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var id))
        {
            await _viewModel.ToggleGroupAsync(id);
        }
    }

    private void AddCustomerAddress_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.OpenCreateCustomerAddress();

    private void EditCustomerAddress_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var id))
        {
            _viewModel.OpenEditCustomerAddress(id);
        }
    }

    private async void SetDefaultCustomerAddress_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var id))
        {
            await _viewModel.SetCustomerAddressDefaultAsync(id);
        }
    }

    private async void ToggleCustomerAddress_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var id))
        {
            await _viewModel.ToggleCustomerAddressAsync(id);
        }
    }

    private async void AddSupplier_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.OpenCreateSupplierAsync();

    private async void SupplierProvince_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, sender))
        {
            return;
        }

        await _viewModel.LoadSupplierWardsAsync();
    }

    private void EditSupplier_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var id))
        {
            _viewModel.OpenEditSupplier(id);
        }
    }

    private async void ToggleSupplier_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var id) && ConfirmStatusChange("nhà cung cấp"))
        {
            await _viewModel.ToggleSupplierAsync(id);
        }
    }

    private async void SupplierDetails_OnClick(object sender, RoutedEventArgs e)
    {
        if (!TryGetTag(sender, out var id))
        {
            return;
        }

        await _viewModel.OpenSupplierDetailsAsync(id);
    }

    private void AddSupplierContact_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.OpenCreateSupplierContact();

    private void EditSupplierContact_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var id))
        {
            _viewModel.OpenEditSupplierContact(id);
        }
    }

    private void AddSupplierAddress_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.OpenCreateSupplierAddress();

    private void EditSupplierAddress_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var id))
        {
            _viewModel.OpenEditSupplierAddress(id);
        }
    }

    private void AddSupplierPaymentTerm_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.OpenCreateSupplierPaymentTerm();

    private void EditSupplierPaymentTerm_OnClick(object sender, RoutedEventArgs e)
    {
        if (TryGetTag(sender, out var id))
        {
            _viewModel.OpenEditSupplierPaymentTerm(id);
        }
    }

    private async void SaveEditor_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.SaveEditorAsync();

    private void CancelEditor_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.CancelEditor();

    private async void ChooseBulkFile_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn tệp khách hàng",
            Filter = "Excel hoặc CSV (*.xlsx;*.csv)|*.xlsx;*.csv|Excel (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) == true)
        {
            await _viewModel.LoadBulkFileAsync(dialog.FileName);
        }
    }

    private void ResetBulkFile_OnClick(object sender, RoutedEventArgs e) =>
        _viewModel.ResetBulkFile();

    private async void PreviewBulk_OnClick(object sender, RoutedEventArgs e) =>
        await _viewModel.PreviewBulkAsync();

    private async void ApplyBulk_OnClick(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Xác nhận thực hiện các dòng hợp lệ đúng theo bản xem trước hiện tại?",
            "Xác nhận dữ liệu khách hàng",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await _viewModel.ApplyBulkAsync();
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
