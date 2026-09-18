using System.Windows;
using System.Windows.Controls;
using CongTy.ApiClient;
using CongTy.Contracts;
using CongTy.Desktop.Partners;
using CongTy.Desktop.Products;
using CongTy.Desktop.Sales;
using Microsoft.Extensions.DependencyInjection;

namespace CongTy.Desktop.Shell;

public sealed class QuickActionWindowService
{
    private readonly IServiceProvider _services;
    private readonly IAccessStateService _accessState;

    public QuickActionWindowService(IServiceProvider services, IAccessStateService accessState)
    {
        _services = services;
        _accessState = accessState;
    }

    public async Task OpenProductsAsync(Window owner)
    {
        var access = new WindowAccessStateService(_accessState);
        var viewModel = ActivatorUtilities.CreateInstance<ProductViewModel>(_services, access);
        var view = new ProductView(viewModel);
        var window = CreateWindow(owner, "Sản phẩm", view, access);
        window.Show();
        await viewModel.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task OpenCustomersAsync(Window owner)
    {
        var access = new WindowAccessStateService(_accessState);
        var viewModel = ActivatorUtilities.CreateInstance<PartnerViewModel>(_services, access);
        var view = new PartnerView(viewModel);
        view.SelectNavigationTarget("customers");
        var window = CreateWindow(owner, "Khách hàng", view, access);
        window.Show();
        await viewModel.EnsureLoadedAsync().ConfigureAwait(true);
    }

    public async Task OpenSalesOrderCreateAsync(Window owner)
    {
        var access = new WindowAccessStateService(_accessState);
        var viewModel = ActivatorUtilities.CreateInstance<SalesViewModel>(_services, access);
        var view = new SalesView(viewModel);
        var window = CreateWindow(owner, "Tạo đơn bán", view, access);
        window.Show();
        await viewModel.EnsureLoadedAsync().ConfigureAwait(true);
        if (window.IsVisible)
        {
            viewModel.OpenQuickCreateEditor();
        }
    }

    private static Window CreateWindow(
        Window owner,
        string title,
        FrameworkElement content,
        WindowAccessStateService access)
    {
        var window = new Window
        {
            Owner = owner,
            Title = $"Công Ty · {title}",
            Width = 1280,
            Height = 800,
            MinWidth = 960,
            MinHeight = 640,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = true,
            Content = content
        };
        window.SetResourceReference(Control.BackgroundProperty, "WindowBackgroundBrush");
        window.Closed += (_, _) => access.Dispose();
        return window;
    }

    private sealed class WindowAccessStateService : IAccessStateService, IDisposable
    {
        private readonly IAccessStateService _source;
        private bool _disposed;

        public WindowAccessStateService(IAccessStateService source)
        {
            _source = source;
            _source.Changed += Source_OnChanged;
        }

        public AccessSnapshot Current => _source.Current;

        public event EventHandler<AccessSnapshot>? Changed;

        public bool HasPermission(string? permission) => _source.HasPermission(permission);

        public bool CanNavigate(string? navigationKey) => _source.CanNavigate(navigationKey);

        public bool CanUseAction(string? requiredPermission) => _source.CanUseAction(requiredPermission);

        public bool TryApply(InternalMeData data, out string errorMessage) =>
            _source.TryApply(data, out errorMessage);

        public void Clear() => _source.Clear();

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _source.Changed -= Source_OnChanged;
        }

        private void Source_OnChanged(object? sender, AccessSnapshot snapshot) =>
            Changed?.Invoke(this, snapshot);
    }
}
