using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;

namespace CongTy.Desktop.Logistics;

public sealed class DeliveryAttemptViewModel : INotifyPropertyChanged
{
    private const string ReadDenied = "Tài khoản chưa được cấp quyền xem Kết quả lần giao.";

    private readonly IDeliveryAttemptService _service;
    private readonly IAccessStateService _access;
    private bool _loaded;
    private Task<bool>? _initialLoadTask;
    private long _accessGeneration;
    private long _loadGeneration;
    private string? _busyAction;
    private string _message = string.Empty;
    private bool _messageIsError;
    private DeliveryAttemptTripRow? _selectedTripRow;
    private string _selectedTripId = string.Empty;
    private int _selectedTripAssignmentCount;

    public DeliveryAttemptViewModel(
        IDeliveryAttemptService service,
        IAccessStateService access)
    {
        _service = service;
        _access = access;

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _loadGeneration++;
            _loaded = false;
            _initialLoadTask = null;
            Reset();
            RaisePermissions();
            if (CanRead && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DeliveryAttemptTripRow> Trips { get; } = [];
    public ObservableCollection<DeliveryAttemptRow> Attempts { get; } = [];

    public bool CanRead =>
        _access.HasPermission("core.delivery-trip.read")
        && _access.HasPermission("core.delivery-attempt.read");

    public bool CanReadProofs =>
        CanRead && _access.HasPermission("core.pod.read");

    public bool IsBusy => _busyAction is not null;
    public bool IsNotBusy => !IsBusy;
    public bool IsLoading => _busyAction == "load";
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool HasTrips => Trips.Count > 0;
    public bool HasSelectedTrip => !string.IsNullOrWhiteSpace(_selectedTripId);
    public bool HasAttempts => Attempts.Count > 0;
    public string RefreshButtonText => IsLoading ? "Đang tải..." : "Tải lại";
    public string SelectedTripNumber => SelectedTripRow?.Number ?? "Chưa chọn chuyến";
    public string ProgressText => HasSelectedTrip
        ? $"{Attempts.Count}/{_selectedTripAssignmentCount} phiếu"
        : string.Empty;

    public string Message
    {
        get => _message;
        private set
        {
            if (SetField(ref _message, value ?? string.Empty))
                OnPropertyChanged(nameof(HasMessage));
        }
    }

    public bool MessageIsError
    {
        get => _messageIsError;
        private set => SetField(ref _messageIsError, value);
    }

    public DeliveryAttemptTripRow? SelectedTripRow
    {
        get => _selectedTripRow;
        set
        {
            if (!SetField(ref _selectedTripRow, value)) return;
            if (value is null)
            {
                _selectedTripId = string.Empty;
                _selectedTripAssignmentCount = 0;
                Attempts.Clear();
                RaiseSelectionState();
                return;
            }
            _ = LoadSummaryAsync(value);
        }
    }

    public async Task<bool> EnsureLoadedAsync()
    {
        if (_loaded) return true;
        if (_initialLoadTask is not null) return await _initialLoadTask.ConfigureAwait(true);
        _initialLoadTask = LoadTripsAsync();
        try
        {
            _loaded = await _initialLoadTask.ConfigureAwait(true);
            return _loaded;
        }
        finally
        {
            _initialLoadTask = null;
        }
    }

    public Task<bool> RefreshAsync() => LoadTripsAsync(_selectedTripId);

    public async Task ToggleProofsAsync(DeliveryAttemptRow? row)
    {
        if (row is null || !HasSelectedTrip || IsBusy) return;
        if (!CanReadProofs)
        {
            SetErrorMessage("Tài khoản chưa được cấp quyền xem bằng chứng giao hàng.");
            return;
        }

        if (row.IsProofExpanded)
        {
            row.IsProofExpanded = false;
            return;
        }

        var tripId = _selectedTripId;
        var requestGeneration = _loadGeneration;
        row.IsProofLoading = true;
        ClearMessage();
        try
        {
            var proofs = await _service.ListProofsAsync(tripId, row.Data.Id).ConfigureAwait(true);
            if (tripId != _selectedTripId || requestGeneration != _loadGeneration) return;
            row.Proofs = proofs.Select(DeliveryAttemptPresentation.Proof).ToArray();
            row.IsProofExpanded = true;
        }
        catch (Exception exception)
        {
            if (tripId == _selectedTripId && requestGeneration == _loadGeneration)
                SetError(exception, "Không tải được bằng chứng giao hàng.");
        }
        finally
        {
            row.IsProofLoading = false;
        }
    }

    private async Task<bool> LoadTripsAsync(string? preferredTripId = null)
    {
        if (!CanRead)
        {
            _loaded = false;
            if (_access.Current.IsAuthenticated) SetErrorMessage(ReadDenied);
            return false;
        }

        var accessGeneration = _accessGeneration;
        var request = ++_loadGeneration;
        SetBusy("load");
        ClearMessage();

        try
        {
            var rows = await _service.ListTripsAsync().ConfigureAwait(true);
            if (accessGeneration != _accessGeneration || request != _loadGeneration || !CanRead) return false;

            var dispatched = rows
                .Where(row => row.Status == "dispatched")
                .Select(DeliveryAttemptPresentation.Trip)
                .ToArray();
            Replace(Trips, dispatched);
            OnPropertyChanged(nameof(HasTrips));

            var selected = string.IsNullOrWhiteSpace(preferredTripId)
                ? null
                : Trips.FirstOrDefault(row => row.Data.Id == preferredTripId);
            _selectedTripRow = selected;
            OnPropertyChanged(nameof(SelectedTripRow));

            if (selected is null)
            {
                _selectedTripId = string.Empty;
                _selectedTripAssignmentCount = 0;
                Attempts.Clear();
                RaiseSelectionState();
            }
            else
            {
                await LoadSummaryAsync(selected, request).ConfigureAwait(true);
            }

            _loaded = true;
            return true;
        }
        catch (Exception exception)
        {
            if (accessGeneration == _accessGeneration && request == _loadGeneration)
                SetError(exception, "Không tải được chuyến đã xuất phát.");
            return false;
        }
        finally
        {
            if (request == _loadGeneration && _busyAction == "load") SetBusy(null);
        }
    }

    private async Task LoadSummaryAsync(DeliveryAttemptTripRow trip, long? parentRequest = null)
    {
        var accessGeneration = _accessGeneration;
        var request = parentRequest ?? ++_loadGeneration;
        var ownsBusy = parentRequest is null;
        _selectedTripId = trip.Data.Id;
        _selectedTripAssignmentCount = trip.Data.AssignmentCount ?? 0;
        Attempts.Clear();
        RaiseSelectionState();

        if (ownsBusy)
        {
            SetBusy($"summary-{trip.Data.Id}");
            ClearMessage();
        }

        try
        {
            var summary = await _service.GetSummaryAsync(trip.Data.Id).ConfigureAwait(true);
            if (accessGeneration != _accessGeneration
                || request != _loadGeneration
                || _selectedTripId != trip.Data.Id
                || !CanRead)
                return;

            Replace(Attempts, summary.Attempts.Select(DeliveryAttemptPresentation.Attempt));
            RaiseSelectionState();
        }
        catch (Exception exception)
        {
            if (accessGeneration == _accessGeneration
                && request == _loadGeneration
                && _selectedTripId == trip.Data.Id)
                SetError(exception, "Không tải được kết quả lần giao.");
        }
        finally
        {
            if (ownsBusy && request == _loadGeneration && _busyAction == $"summary-{trip.Data.Id}")
                SetBusy(null);
        }
    }

    private void Reset()
    {
        Trips.Clear();
        Attempts.Clear();
        _selectedTripRow = null;
        _selectedTripId = string.Empty;
        _selectedTripAssignmentCount = 0;
        ClearMessage();
        OnPropertyChanged(nameof(SelectedTripRow));
        OnPropertyChanged(nameof(HasTrips));
        RaiseSelectionState();
    }

    private void SetBusy(string? action)
    {
        if (string.Equals(_busyAction, action, StringComparison.Ordinal)) return;
        _busyAction = action;
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(RefreshButtonText));
    }

    private void RaisePermissions()
    {
        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanReadProofs));
    }

    private void RaiseSelectionState()
    {
        OnPropertyChanged(nameof(HasSelectedTrip));
        OnPropertyChanged(nameof(HasAttempts));
        OnPropertyChanged(nameof(SelectedTripNumber));
        OnPropertyChanged(nameof(ProgressText));
    }

    private void ClearMessage()
    {
        MessageIsError = false;
        Message = string.Empty;
    }

    private void SetErrorMessage(string value)
    {
        MessageIsError = true;
        Message = value;
    }

    private void SetError(Exception exception, string fallback)
    {
        if (exception is CanonicalApiException api)
        {
            SetErrorMessage(CanonicalErrorMessages.WithRequestId(
                CanonicalErrorMessages.ToOfficeMessage(api),
                api.RequestId));
            return;
        }

        SetErrorMessage(string.IsNullOrWhiteSpace(exception.Message) ? fallback : exception.Message);
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var value in values) target.Add(value);
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess()) action();
        else dispatcher.Invoke(action);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
