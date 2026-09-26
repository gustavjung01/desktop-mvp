using CongTy.ApiClient;

namespace CongTy.Desktop.Sales
{
    public partial class OrderManagementView
    {
        private async void ExportXlsx_OnClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_viewModel is null) return;
            await CongTy.Desktop.Operations.OfficeExportDialog.RunAsync(
                this,
                () => Task.FromResult<ApiDownloadFile?>(_viewModel.ExportOperational("xlsx")));
        }

        private async void ExportCsv_OnClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_viewModel is null) return;
            await CongTy.Desktop.Operations.OfficeExportDialog.RunAsync(
                this,
                () => Task.FromResult<ApiDownloadFile?>(_viewModel.ExportOperational("csv")));
        }
    }
}

namespace CongTy.Desktop.Purchasing
{
    public partial class GoodsReceiptView
    {
        private async void ExportXlsx_OnClick(object sender, System.Windows.RoutedEventArgs e) =>
            await CongTy.Desktop.Operations.OfficeExportDialog.RunAsync(this, () => Task.FromResult<ApiDownloadFile?>(_viewModel.ExportOperational("xlsx")));

        private async void ExportCsv_OnClick(object sender, System.Windows.RoutedEventArgs e) =>
            await CongTy.Desktop.Operations.OfficeExportDialog.RunAsync(this, () => Task.FromResult<ApiDownloadFile?>(_viewModel.ExportOperational("csv")));
    }

    public partial class SupplierReturnView
    {
        private async void ExportXlsx_OnClick(object sender, System.Windows.RoutedEventArgs e) =>
            await CongTy.Desktop.Operations.OfficeExportDialog.RunAsync(this, () => Task.FromResult<ApiDownloadFile?>(_viewModel.ExportOperational("xlsx")));

        private async void ExportCsv_OnClick(object sender, System.Windows.RoutedEventArgs e) =>
            await CongTy.Desktop.Operations.OfficeExportDialog.RunAsync(this, () => Task.FromResult<ApiDownloadFile?>(_viewModel.ExportOperational("csv")));
    }

    public partial class PurchasePriceView
    {
        private async void ExportXlsx_OnClick(object sender, System.Windows.RoutedEventArgs e) =>
            await CongTy.Desktop.Operations.OfficeExportDialog.RunAsync(this, () => Task.FromResult<ApiDownloadFile?>(_viewModel.ExportOperational()));
    }
}

namespace CongTy.Desktop.Logistics
{
    public partial class CustomerReturnView
    {
        private async void ExportXlsx_OnClick(object sender, System.Windows.RoutedEventArgs e) =>
            await CongTy.Desktop.Operations.OfficeExportDialog.RunAsync(this, () => Task.FromResult<ApiDownloadFile?>(_viewModel.ExportOperational("xlsx")));

        private async void ExportCsv_OnClick(object sender, System.Windows.RoutedEventArgs e) =>
            await CongTy.Desktop.Operations.OfficeExportDialog.RunAsync(this, () => Task.FromResult<ApiDownloadFile?>(_viewModel.ExportOperational("csv")));
    }

    public partial class DeliveryAttemptView
    {
        private async void ExportXlsx_OnClick(object sender, System.Windows.RoutedEventArgs e) =>
            await CongTy.Desktop.Operations.OfficeExportDialog.RunAsync(this, () => Task.FromResult<ApiDownloadFile?>(_viewModel.ExportSelectedTrip()));
    }
}

namespace CongTy.Desktop.Accounting
{
    public partial class CustomerPaymentView
    {
        private async void ExportXlsx_OnClick(object sender, System.Windows.RoutedEventArgs e) =>
            await CongTy.Desktop.Operations.OfficeExportDialog.RunAsync(this, () => Task.FromResult<ApiDownloadFile?>(ViewModel.ExportOperational()));
    }

    public partial class SupplierPaymentView
    {
        private async void ExportXlsx_OnClick(object sender, System.Windows.RoutedEventArgs e) =>
            await CongTy.Desktop.Operations.OfficeExportDialog.RunAsync(this, () => Task.FromResult<ApiDownloadFile?>(ViewModel.ExportOperational()));
    }

    public partial class CustomerReturnCreditView
    {
        private async void ExportXlsx_OnClick(object sender, System.Windows.RoutedEventArgs e) =>
            await CongTy.Desktop.Operations.OfficeExportDialog.RunAsync(this, () => Task.FromResult<ApiDownloadFile?>(ViewModel.ExportOperational()));
    }

    public partial class ReceivablesView
    {
        private async void ExportXlsx_OnClick(object sender, System.Windows.RoutedEventArgs e) =>
            await CongTy.Desktop.Operations.OfficeExportDialog.RunAsync(this, () => Task.FromResult<ApiDownloadFile?>(ViewModel.ExportOperational()));
    }

    public partial class PayablesView
    {
        private async void ExportXlsx_OnClick(object sender, System.Windows.RoutedEventArgs e) =>
            await CongTy.Desktop.Operations.OfficeExportDialog.RunAsync(this, () => Task.FromResult<ApiDownloadFile?>(ViewModel.ExportOperational()));
    }
}
