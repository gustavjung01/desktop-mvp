using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CongTy.ApiClient;
using CongTy.Desktop.Accounting;

namespace CongTy.Desktop.Shell;

public partial class MainWindow
{
    private bool _receivablesShellWired;

    private void WireReceivablesWorkspace()
    {
        if (_receivablesShellWired) return;
        var workspaceTabs = FindLogicalParent<TabControl>(HomeHost);
        if (workspaceTabs is null) return;

        _viewModel.InitializeReceivablesShell();
        var app = (CongTy.Desktop.App)Application.Current;
        var service = new ReceivablesService(
            app.ResolveRequired<CompanyApiClient>(),
            app.ResolveRequired<IAuthenticatedSessionAccessor>());
        var viewModel = new ReceivablesViewModel(
            service,
            app.ResolveRequired<IAccessStateService>());
        var view = new ReceivablesView(viewModel);

        while (workspaceTabs.Items.Count <= 39)
            workspaceTabs.Items.Add(new TabItem());
        workspaceTabs.Items[39] = new TabItem { Content = view };

        WireReceivablesShortcutButtons(this);
        if (AgingReportingHost.Content is DependencyObject agingRoot)
            WireReceivablesShortcutButtons(agingRoot);

        _receivablesShellWired = true;
    }

    private void WireReceivablesShortcutButtons(DependencyObject root)
    {
        foreach (var button in FindVisualChildren<Button>(root)
                     .Where(button =>
                         string.Equals(button.Content?.ToString(), "Công nợ phải thu", StringComparison.Ordinal)
                         || string.Equals(button.Content?.ToString(), "Mở công nợ phải thu", StringComparison.Ordinal)))
        {
            BindingOperations.SetBinding(
                button,
                Button.IsEnabledProperty,
                new Binding(nameof(ShellViewModel.CanViewReceivables))
                {
                    Source = _viewModel
                });
            button.Click -= Receivables_OnClick;
            button.Click += Receivables_OnClick;
        }
    }

    private async void Receivables_OnClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.NavigateReceivablesAsync();
        ApplyReceivablesHeader();
    }

    private void ApplyReceivablesHeader()
    {
        if (!_viewModel.IsReceivablesSelected) return;

        SetShellHeaderText(nameof(ShellViewModel.HeaderKicker), "KẾ TOÁN & CÔNG NỢ");
        SetShellHeaderText(nameof(ShellViewModel.PageTitle), "Công nợ phải thu");
        SetShellHeaderText(
            nameof(ShellViewModel.PageSubtitle),
            "Xem số tiền khách còn nợ và lần giao hàng hoặc nhận tại quầy đã phát sinh khoản nợ.");
    }

    private void SetShellHeaderText(string bindingPath, string value)
    {
        foreach (var textBlock in FindVisualChildren<TextBlock>(this))
        {
            var binding = BindingOperations.GetBinding(textBlock, TextBlock.TextProperty);
            if (!string.Equals(binding?.Path?.Path, bindingPath, StringComparison.Ordinal))
                continue;
            textBlock.SetCurrentValue(TextBlock.TextProperty, value);
        }
    }
}
