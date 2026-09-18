using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CongTy.Desktop.Printing;

namespace CongTy.Desktop.Sales;

public partial class SalesView : UserControl
{
    private readonly SalesViewModel _viewModel;
    private CancellationTokenSource? _skuSearchCts;
    private CancellationTokenSource? _customerSearchCts;

    public SalesView(SalesViewModel viewModel)
    {
        _viewModel=viewModel;
        InitializeComponent();
        DataContext=viewModel;
    }

    private async void Refresh_OnClick(object sender,RoutedEventArgs e)=>await _viewModel.RefreshAsync();
    private void Create_OnClick(object sender,RoutedEventArgs e)=>_viewModel.OpenCreateEditor();
    private void CopyOrder_OnClick(object sender,RoutedEventArgs e)=>_viewModel.OpenCopyEditor();
    private async void PrintOrder_OnClick(object sender,RoutedEventArgs e)
    {
        if(_viewModel.SelectedOrder is not { } order||_viewModel.SelectedVersion is not { } version)return;
        var owner=Window.GetWindow(this);
        var template=await DocumentPrintTemplateRuntime.LoadForPrintAsync(owner,"SALES_ORDER");
        if(template is null)return;
        SalesOrderPrintPreview.Show(owner,order,version,template);
    }
    private async void OpenOrder_OnClick(object sender,RoutedEventArgs e){if(sender is Button{Tag:string id})await _viewModel.SelectOrderAsync(id);}
    private void EditDraft_OnClick(object sender,RoutedEventArgs e)=>_viewModel.OpenDraftEditor();
    private async void Confirm_OnClick(object sender,RoutedEventArgs e)=>await _viewModel.ConfirmSelectedAsync();
    private void EditManual_OnClick(object sender,RoutedEventArgs e)=>_viewModel.OpenManualEditor();
    private async void IssueStock_OnClick(object sender,RoutedEventArgs e)=>await _viewModel.IssueStockAsync();
    private async void CreateAmendment_OnClick(object sender,RoutedEventArgs e)=>await _viewModel.CreateAmendmentAsync();
    private void EditAmendment_OnClick(object sender,RoutedEventArgs e)=>_viewModel.OpenAmendmentEditor();
    private async void ConfirmAmendment_OnClick(object sender,RoutedEventArgs e)=>await _viewModel.ConfirmAmendmentSelectedAsync();

    private async void CancelOrder_OnClick(object sender,RoutedEventArgs e)
    {
        if(MessageBox.Show("Xác nhận hủy đơn bán hàng này?","Hủy đơn bán hàng",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)
            await _viewModel.CancelSelectedAsync();
    }

    private async void CloseExecution_OnClick(object sender,RoutedEventArgs e)
    {
        if(MessageBox.Show("Kết thúc phần chưa giao và giữ nguyên lịch sử đã thực hiện?","Kết thúc phần chưa giao",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes)
            await _viewModel.CloseExecutionAsync();
    }

    private async void CompleteDirect_OnClick(object sender,RoutedEventArgs e)=>await _viewModel.CompleteDirectAsync();
    private async void SettleDirect_OnClick(object sender,RoutedEventArgs e)=>await _viewModel.SettleDirectAsync();
    private void FillRemaining_OnClick(object sender,RoutedEventArgs e)=>_viewModel.FillRemainingAmount();
    private void CancelEditor_OnClick(object sender,RoutedEventArgs e)
    {
        if(_viewModel.HasUnsavedEditorChanges()
            &&MessageBox.Show("Đơn có thay đổi chưa lưu. Đóng và bỏ thay đổi?","Đóng đơn đang nhập",MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)
            return;
        _viewModel.CancelEditor();
    }
    private void QuickCustomerOpen_OnClick(object sender,RoutedEventArgs e)=>_viewModel.OpenQuickCustomer();
    private void QuickCustomerClose_OnClick(object sender,RoutedEventArgs e)=>_viewModel.CloseQuickCustomer();
    private void MoreInfo_OnClick(object sender,RoutedEventArgs e)=>MoreInfoPopup.IsOpen=!MoreInfoPopup.IsOpen;
    private async void QuickCustomerCreate_OnClick(object sender,RoutedEventArgs e)=>await _viewModel.CreateQuickCustomerAsync();
    private void CustomerResult_OnClick(object sender,RoutedEventArgs e){if(sender is Button{Tag:string id})_viewModel.SelectCustomer(id);}
    private async void SearchSku_OnClick(object sender,RoutedEventArgs e)=>await _viewModel.SearchSkuAsync();
    private async void AddSku_OnClick(object sender,RoutedEventArgs e)
    {
        if(sender is not Button{Tag:string id})return;
        var line=await _viewModel.AddSkuAsync(id);
        if(line is not null)FocusQuantity(line);
    }

    private async void CustomerSearch_OnTextChanged(object sender,TextChangedEventArgs e)
    {
        CustomerResultsList.SelectedIndex=-1;
        _customerSearchCts?.Cancel(); _customerSearchCts?.Dispose(); _customerSearchCts=new CancellationTokenSource(); var token=_customerSearchCts.Token;
        try { await Task.Delay(180,token); await _viewModel.SearchCustomersAsync(token); } catch(OperationCanceledException) when(token.IsCancellationRequested){ }
    }

    private void CustomerSearch_OnPreviewKeyDown(object sender,KeyEventArgs e)
    {
        if(e.Key is Key.Down or Key.Up)
        {
            e.Handled=true; MoveListSelection(CustomerResultsList,e.Key==Key.Down?1:-1); return;
        }
        if(e.Key!=Key.Enter)return;
        var selected=CustomerResultsList.SelectedItem as SalesLookupOption??_viewModel.CustomerOptions.FirstOrDefault();
        if(selected is null)return;
        e.Handled=true; _viewModel.SelectCustomer(selected.Id);
    }

    private void CustomerResults_OnMouseLeftButtonUp(object sender,MouseButtonEventArgs e)
    {
        if(CustomerResultsList.SelectedItem is SalesLookupOption selected)_viewModel.SelectCustomer(selected.Id);
    }

    private async void SkuSearch_OnTextChanged(object sender,TextChangedEventArgs e)
    {
        SkuResultsList.SelectedIndex=-1;
        _skuSearchCts?.Cancel(); _skuSearchCts?.Dispose(); _skuSearchCts=new CancellationTokenSource(); var token=_skuSearchCts.Token;
        try { await Task.Delay(120,token); await _viewModel.SearchSkuAsync(token); } catch(OperationCanceledException) when(token.IsCancellationRequested){ }
    }

    private async void SkuSearch_OnPreviewKeyDown(object sender,KeyEventArgs e)
    {
        if(e.Key is Key.Down or Key.Up)
        {
            e.Handled=true;
            MoveListSelection(SkuResultsList,e.Key==Key.Down?1:-1,item=>item is SalesSkuSearchRow row&&row.Selectable);
            return;
        }
        if(e.Key!=Key.Enter)return;
        var selected=SkuResultsList.SelectedItem as SalesSkuSearchRow;
        if(selected is null||!selected.Selectable)selected=_viewModel.SkuRows.FirstOrDefault(x=>x.Selectable);
        if(selected is null)return;
        e.Handled=true;
        var line=await _viewModel.AddSkuAsync(selected.Id);
        if(line is not null)FocusQuantity(line);
    }

    private async void SkuResults_OnMouseLeftButtonUp(object sender,MouseButtonEventArgs e)
    {
        if(SkuResultsList.SelectedItem is not SalesSkuSearchRow{Selectable:true} selected)return;
        var line=await _viewModel.AddSkuAsync(selected.Id);
        if(line is not null)FocusQuantity(line);
    }

    private async void UnitCombo_OnDropDownOpened(object sender,EventArgs e)
    {
        if(sender is ComboBox{DataContext:SalesDraftLineRow row})await _viewModel.LoadLineVariantsAsync(row);
    }

    private async void UnitCombo_OnSelectionChanged(object sender,SelectionChangedEventArgs e)
    {
        if(sender is ComboBox{DataContext:SalesDraftLineRow row,SelectedValue:string id}&&id!=row.VariantId)
            await _viewModel.ChangeLineVariantAsync(row,id);
    }

    private async void QuantityMinus_OnClick(object sender,RoutedEventArgs e)
    {
        if(sender is Button{Tag:SalesDraftLineRow row}){await _viewModel.AdjustQuantityAsync(row,-1);FocusQuantity(row);}
    }

    private async void QuantityPlus_OnClick(object sender,RoutedEventArgs e)
    {
        if(sender is Button{Tag:SalesDraftLineRow row}){await _viewModel.AdjustQuantityAsync(row,1);FocusQuantity(row);}
    }

    private async void LineQuantity_OnPreviewKeyDown(object sender,KeyEventArgs e)
    {
        if(sender is not TextBox{DataContext:SalesDraftLineRow row})return;
        if(e.Key==Key.Up){e.Handled=true;await _viewModel.AdjustQuantityAsync(row,1);FocusQuantity(row);return;}
        if(e.Key==Key.Down){e.Handled=true;await _viewModel.AdjustQuantityAsync(row,-1);FocusQuantity(row);return;}
        if(e.Key==Key.Tab&&Keyboard.Modifiers==ModifierKeys.None){e.Handled=true;FocusPrice(row);}
    }

    private async void LineQuantity_OnLostKeyboardFocus(object sender,KeyboardFocusChangedEventArgs e)
    {
        if(sender is TextBox{DataContext:SalesDraftLineRow row})await _viewModel.RepriceLineAsync(row);
    }

    private void UseSystemPrice_OnClick(object sender,RoutedEventArgs e)
    {
        if(sender is Button{Tag:SalesDraftLineRow row})_viewModel.UseSystemPrice(row);
    }

    private async void SplitLine_OnClick(object sender,RoutedEventArgs e)
    {
        if(sender is not Button{Tag:SalesDraftLineRow row})return;
        var split=await _viewModel.SplitLineAsync(row);
        if(split is not null)FocusUnit(split);
    }

    private void TogglePriceDetail_OnClick(object sender,RoutedEventArgs e)
    {
        if(sender is Button{Tag:SalesDraftLineRow row})_viewModel.TogglePriceDetail(row);
    }

    private async void OpenInventoryHistory_OnClick(object sender,RoutedEventArgs e)
    {
        if(sender is Button{Tag:SalesDraftLineRow row})await _viewModel.OpenInventoryHistoryAsync(row);
    }

    private void CloseInventoryHistory_OnClick(object sender,RoutedEventArgs e)=>_viewModel.CloseInventoryHistory();

    private void SelectAllTextBox_OnGotKeyboardFocus(object sender,KeyboardFocusChangedEventArgs e)
    {
        if(sender is TextBox textBox)textBox.SelectAll();
    }

    private void SelectAllTextBox_OnPreviewMouseLeftButtonDown(object sender,MouseButtonEventArgs e)
    {
        if(sender is not TextBox textBox||textBox.IsKeyboardFocusWithin)return;
        e.Handled=true; textBox.Focus(); textBox.SelectAll();
    }

    private void EditorOverlay_OnPreviewKeyDown(object sender,KeyEventArgs e)
    {
        if(_viewModel.QuickCustomerOpen)return;
        if(e.Key==Key.F3){e.Handled=true;FocusAndSelect(SkuSearchBox);return;}
        if(e.Key==Key.F4){e.Handled=true;FocusAndSelect(CustomerSearchBox);}
    }

    private static void FocusAndSelect(TextBox textBox)
    {
        textBox.Focus(); textBox.SelectAll();
    }

    private void FocusQuantity(SalesDraftLineRow line)=>FocusLineTextBox(line,"QuantityInput");

    private void FocusPrice(SalesDraftLineRow line)=>FocusLineTextBox(line,"PriceInput");

    private void FocusLineTextBox(SalesDraftLineRow line,string uid)
    {
        Dispatcher.BeginInvoke(() =>
        {
            OrderLinesGrid.SelectedItem=line;
            OrderLinesGrid.ScrollIntoView(line);
            OrderLinesGrid.UpdateLayout();
            if(OrderLinesGrid.ItemContainerGenerator.ContainerFromItem(line) is not DataGridRow row)return;
            var input=FindVisualChild<TextBox>(row,textBox=>textBox.Uid==uid);
            if(input is null||!input.IsEnabled)return;
            input.Focus(); input.SelectAll();
        },DispatcherPriority.Input);
    }

    private void FocusUnit(SalesDraftLineRow line)
    {
        Dispatcher.BeginInvoke(() =>
        {
            OrderLinesGrid.SelectedItem=line;
            OrderLinesGrid.ScrollIntoView(line);
            OrderLinesGrid.UpdateLayout();
            if(OrderLinesGrid.ItemContainerGenerator.ContainerFromItem(line) is not DataGridRow row)return;
            var combo=FindVisualChild<ComboBox>(row,item=>item.Uid=="UnitInput");
            combo?.Focus();
        },DispatcherPriority.Input);
    }

    private static void MoveListSelection(ListBox list,int delta,Func<object,bool>? selectable=null)
    {
        if(list.Items.Count==0)return;
        var index=list.SelectedIndex;
        var next=index;
        for(var attempt=0;attempt<list.Items.Count;attempt++)
        {
            next+=delta;
            if(next<0)next=list.Items.Count-1;
            if(next>=list.Items.Count)next=0;
            var item=list.Items[next];
            if(selectable is not null&&!selectable(item))continue;
            list.SelectedIndex=next;
            list.ScrollIntoView(item);
            return;
        }
    }

    private static T? FindVisualChild<T>(DependencyObject root,Func<T,bool> predicate) where T:DependencyObject
    {
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)
        {
            var child=VisualTreeHelper.GetChild(root,i);
            if(child is T typed&&predicate(typed))return typed;
            var nested=FindVisualChild(child,predicate);
            if(nested is not null)return nested;
        }
        return null;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T:DependencyObject
    {
        for(var i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)
        {
            var child=VisualTreeHelper.GetChild(root,i);
            if(child is T typed)yield return typed;
            foreach(var nested in FindVisualChildren<T>(child))yield return nested;
        }
    }

    private void RemoveLine_OnClick(object sender,RoutedEventArgs e){if(sender is Button{Tag:string id})_viewModel.RemoveDraftLine(id);}
    private async void SaveDraft_OnClick(object sender,RoutedEventArgs e)=>await _viewModel.SaveEditorAsync(false);
    private async void SaveConfirm_OnClick(object sender,RoutedEventArgs e)=>await _viewModel.SaveEditorAsync(true);
}
