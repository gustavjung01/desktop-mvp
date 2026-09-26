using CongTy.ApiClient;

namespace CongTy.Desktop.Purchasing
{
    public partial class PurchasingReportingView
    {
        private async void ExportXlsx_OnClick(object sender, System.Windows.RoutedEventArgs e) =>
            await CongTy.Desktop.Operations.OfficeExportDialog.RunAsync(
                this,
                () => Task.FromResult<ApiDownloadFile?>(_viewModel.ExportReport()));
    }
}

namespace CongTy.Desktop.Accounting
{
    public partial class AgingReportingView
    {
        private async void ExportXlsx_OnClick(object sender, System.Windows.RoutedEventArgs e) =>
            await CongTy.Desktop.Operations.OfficeExportDialog.RunAsync(
                this,
                () => Task.FromResult<ApiDownloadFile?>(_viewModel.ExportReport()));
    }

    public partial class CodAccountingView
    {
        private async void ExportXlsx_OnClick(object sender, System.Windows.RoutedEventArgs e) =>
            await CongTy.Desktop.Operations.OfficeExportDialog.RunAsync(
                this,
                () => Task.FromResult<ApiDownloadFile?>(_viewModel.ExportReport()));
    }
}

namespace CongTy.Desktop.Logistics
{
    public partial class LogisticsReportingView
    {
        private async void ExportXlsx_OnClick(object sender, System.Windows.RoutedEventArgs e) =>
            await CongTy.Desktop.Operations.OfficeExportDialog.RunAsync(
                this,
                () => Task.FromResult<ApiDownloadFile?>(_viewModel.ExportReport()));
    }
}

namespace CongTy.Desktop.Settings
{
    public partial class EmployeeMcpReportingView
    {
        private async void ExportXlsx_OnClick(object sender, System.Windows.RoutedEventArgs e) =>
            await CongTy.Desktop.Operations.OfficeExportDialog.RunAsync(
                this,
                () => Task.FromResult<ApiDownloadFile?>(ViewModel.ExportReport()));
    }
}
