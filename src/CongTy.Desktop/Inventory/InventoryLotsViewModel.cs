using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Inventory;

public sealed class InventoryLotsViewModel : INotifyPropertyChanged
{
    private readonly IInventoryService _service;
    private readonly IAccessStateService _access;
    private readonly List<InventoryLotData> _source = [];

    private bool _loaded;
    private long _accessGeneration;
    private Task<bool>? _initialLoadTask;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private string _search = string.Empty;

    public InventoryLotsViewModel(
        IInventoryService service,
        IAccessStateService access)
    {
        _service = service;
        _access = access;

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _loaded = false;
            _initialLoadTask = null;
            _source.Clear();
            Rows.Clear();
            Message = string.Empty;
            MessageIsError = false;
            OnPropertyChanged(nameof(CanRead));
            OnPropertyChanged(nameof(HasRows));
            OnPropertyChanged(nameof(HasNoRows));

            if (CanRead && _access.Current.IsAuthenticated)
            {
                _ = EnsureLoadedAsync();
            }
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<InventoryLotsRow> Rows { get; } = [];

    public bool CanRead => _access.HasPermission("core.inventory.lot.read");
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            OnPropertyChanged(nameof(IsNotBusy));
            OnPropertyChanged(nameof(RefreshText));
            OnPropertyChanged(nameof(HasNoRows));
        }
    }

    public bool IsNotBusy => !IsBusy;
    public bool HasRows => Rows.Count > 0;
    public bool HasNoRows => !IsBusy && Rows.Count == 0;
    public string RefreshText => IsBusy ? "Đang làm mới..." : "Làm mới dữ liệu";

    public string Message
    {
        get => _message;
        private set => SetField(ref _message, value);
    }

    public bool MessageIsError
    {
        get => _messageIsError;
        private set => SetField(ref _messageIsError, value);
    }

    public string Search
    {
        get => _search;
        set
        {
            if (!SetField(ref _search, value ?? string.Empty)) return;
            ApplyFilter();
        }
    }

    public async Task<bool> EnsureLoadedAsync()
    {
        if (_loaded) return true;
        if (_initialLoadTask is not null)
        {
            return await _initialLoadTask.ConfigureAwait(true);
        }

        _initialLoadTask = LoadAsync();
        try
        {
            return await _initialLoadTask.ConfigureAwait(true);
        }
        finally
        {
            _initialLoadTask = null;
        }
    }

    public Task<bool> RefreshAsync() => LoadAsync();

    private async Task<bool> LoadAsync()
    {
        if (!CanRead)
        {
            SetError("Tài khoản chưa được cấp quyền xem Lô hàng.");
            return false;
        }

        var generation = _accessGeneration;
        IsBusy = true;
        ClearMessage();
        try
        {
            var data = await _service.ListLotsAsync().ConfigureAwait(true);
            if (generation != _accessGeneration || !CanRead) return false;

            _source.Clear();
            _source.AddRange(data);
            ApplyFilter();
            _loaded = true;
            SetNotice("Dữ liệu đã được làm mới.");
            return true;
        }
        catch (CanonicalApiException exception)
        {
            SetError(CanonicalErrorMessages.WithRequestId(
                CanonicalErrorMessages.ToOfficeMessage(exception),
                exception.RequestId));
            return false;
        }
        catch
        {
            SetError("Không tải được danh sách lô hàng. Vui lòng thử lại.");
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilter()
    {
        var term = Search.Trim().ToLowerInvariant();
        var filtered = _source
            .Where(row => term.Length == 0
                || InventoryLotsPresentation.SearchText(row)
                    .Contains(term, StringComparison.Ordinal))
            .Select((row, index) => new InventoryLotsRow(index + 1, row));

        Rows.Clear();
        foreach (var row in filtered)
        {
            Rows.Add(row);
        }

        OnPropertyChanged(nameof(HasRows));
        OnPropertyChanged(nameof(HasNoRows));
    }

    private void ClearMessage()
    {
        MessageIsError = false;
        Message = string.Empty;
    }

    private void SetNotice(string message)
    {
        MessageIsError = false;
        Message = message;
    }

    private void SetError(string message)
    {
        MessageIsError = true;
        Message = message;
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            dispatcher.Invoke(action);
        }
    }

    private bool SetField<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
