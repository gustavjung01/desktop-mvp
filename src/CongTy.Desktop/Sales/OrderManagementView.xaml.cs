using System.Windows;
using System.Windows.Controls;
using CongTy.ApiClient;
using CongTy.Desktop.Shell;

namespace CongTy.Desktop.Sales;

public partial class OrderManagementView : UserControl
{
    private OrderManagementViewModel? _viewModel;

    public OrderManagementView()
    {
        InitializeComponent();
        Loaded+=OrderManagementView_OnLoaded;
    }

    private async void OrderManagementView_OnLoaded(object sender,RoutedEventArgs e)
    {
        if(_viewModel is null)
        {
            if(Application.Current is not App app)return;
            var service=new OrderManagementQueryService(
                app.ResolveRequired<CompanyApiClient>(),
                app.ResolveRequired<IAuthenticatedSessionAccessor>());
            _viewModel=new OrderManagementViewModel(service,app.ResolveRequired<IAccessStateService>());
            DataContext=_viewModel;
        }
        await _viewModel.EnsureLoadedAsync();
    }

    private async void Refresh_OnClick(object sender,RoutedEventArgs e)
    {
        if(_viewModel is not null)await _viewModel.RefreshAsync();
    }

    private void StageCard_OnClick(object sender,RoutedEventArgs e)
    {
        if(_viewModel is not null&&sender is Button{Tag:string stage})_viewModel.SetStage(stage);
    }

    private void RowSelection_OnClick(object sender,RoutedEventArgs e)
    {
        if(_viewModel is null||sender is not CheckBox{Tag:string id} checkBox)return;
        _viewModel.ToggleSelection(id,checkBox.IsChecked==true);
    }

    private void SelectAll_OnClick(object sender,RoutedEventArgs e)=>_viewModel?.SelectAllFiltered();
    private void ClearSelection_OnClick(object sender,RoutedEventArgs e)=>_viewModel?.ClearSelection();
    private void PreviousPage_OnClick(object sender,RoutedEventArgs e)=>_viewModel?.PreviousPage();
    private void NextPage_OnClick(object sender,RoutedEventArgs e)=>_viewModel?.NextPage();

    private async void PrintSelected_OnClick(object sender,RoutedEventArgs e)
    {
        if(_viewModel is null)return;
        var items=await _viewModel.LoadPrintableSelectionAsync();
        if(items.Count==0)return;
        SalesOrderPrintPreview.ShowBatch(Window.GetWindow(this),items);
    }

    private async void CreateOrder_OnClick(object sender,RoutedEventArgs e)
    {
        var main=Window.GetWindow(this) as MainWindow;
        if(main?.DataContext is not ShellViewModel shell)return;
        await shell.NavigateSalesAsync();
        if(main.FindName("SalesHost") is ContentControl{Content:SalesView salesView}
            && salesView.DataContext is SalesViewModel salesViewModel)
            salesViewModel.OpenCreateEditor();
    }

    private async void OpenOrder_OnClick(object sender,RoutedEventArgs e)
    {
        if(sender is not Button{Tag:string id})return;
        var main=Window.GetWindow(this) as MainWindow;
        if(main?.DataContext is not ShellViewModel shell)return;
        await shell.NavigateSalesAsync();
        if(main.FindName("SalesHost") is ContentControl{Content:SalesView salesView}
            && salesView.DataContext is SalesViewModel salesViewModel)
            await salesViewModel.SelectOrderAsync(id);
    }
}
