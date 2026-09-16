using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Logistics;

public sealed class TripPlanningViewModel : INotifyPropertyChanged
{
    private const int IntentCacheLimit = 256;
    private const string ReadDenied = "Tài khoản chưa được cấp quyền xem Lập và xếp chuyến.";

    private readonly ITripPlanningService _service;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly Dictionary<string, string> _intentKeys = new(StringComparer.Ordinal);
    private readonly List<TripWarehouseData> _warehouses = [];
    private readonly List<LogisticsRouteData> _routes = [];
    private readonly List<LogisticsVehicleData> _vehicles = [];
    private readonly List<LogisticsDriverData> _drivers = [];
    private readonly List<LogisticsDriverEmployeeData> _driverEmployees = [];
    private readonly List<TripEligibleDeliveryOrderData> _eligible = [];
    private readonly List<DeliveryTripData> _trips = [];

    private bool _loaded;
    private bool _driverEmployeesLoaded;
    private bool _driverEmployeesBusy;
    private long _accessGeneration;
    private long _loadGeneration;
    private Task<bool>? _initialLoadTask;
    private string? _busyAction;
    private string _message = string.Empty;
    private bool _messageIsError;
    private int _activeTabIndex;
    private int _planningTabIndex;

    private string _routeCode = string.Empty;
    private string _routeName = string.Empty;
    private string _routeDescription = string.Empty;
    private string _routeWarehouseId = string.Empty;
    private string _vehicleCode = string.Empty;
    private string _vehiclePlate = string.Empty;
    private string _vehicleType = string.Empty;
    private string _driverEmployeeId = string.Empty;
    private string _driverLicenseReference = string.Empty;

    private string _tripWarehouseId = string.Empty;
    private string _tripRouteId = string.Empty;
    private string _tripVehicleId = string.Empty;
    private string _tripDriverId = string.Empty;
    private string _plannedStartText = string.Empty;
    private string _tripNote = string.Empty;

    private DeliveryTripListRow? _selectedTripRow;
    private DeliveryTripData? _selectedTrip;
    private string _assignmentRouteId = string.Empty;
    private string _assignmentTripId = string.Empty;

    public TripPlanningViewModel(
        ITripPlanningService service,
        IAccessStateService access,
        ICanonicalIdempotencyKeyProvider idempotencyKeys)
    {
        _service = service;
        _access = access;
        _idempotencyKeys = idempotencyKeys;

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _accessGeneration++;
            _loadGeneration++;
            _loaded = false;
            _driverEmployeesLoaded = false;
            _initialLoadTask = null;
            Reset();
            RaisePermissions();
            if (CanRead && _access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<TripOption> WarehouseOptions { get; } = [];
    public ObservableCollection<TripOption> RouteWarehouseOptions { get; } = [];
    public ObservableCollection<TripOption> TripRouteOptions { get; } = [];
    public ObservableCollection<TripOption> VehicleOptions { get; } = [];
    public ObservableCollection<TripOption> DriverOptions { get; } = [];
    public ObservableCollection<TripOption> DriverEmployeeOptions { get; } = [];
    public ObservableCollection<DeliveryTripListRow> Trips { get; } = [];
    public ObservableCollection<TripStopRow> Stops { get; } = [];
    public ObservableCollection<TripOption> AssignmentRouteOptions { get; } = [];
    public ObservableCollection<TripOption> AssignmentTripOptions { get; } = [];
    public ObservableCollection<EligibleDeliveryOrderRow> EligibleOrders { get; } = [];

    public bool CanRead => _access.HasPermission("core.delivery-trip.read");
    public bool CanManageRoutes => _access.HasPermission("core.logistics-route.manage");
    public bool CanManageVehicles => _access.HasPermission("core.vehicle.manage");
    public bool CanReadDrivers => _access.HasPermission("core.driver-profile.read");
    public bool CanManageDrivers => _access.HasPermission("core.driver-profile.manage");
    public bool CanCreateTrip => _access.HasPermission("core.delivery-trip.create");
    public bool CanPlanTrip => _access.HasPermission("core.delivery-trip.plan");
    public bool CanAssignTrip => _access.HasPermission("core.delivery-trip.assign");
    public bool CanLockTrip => _access.HasPermission("core.delivery-trip.lock");

    public bool IsBusy => _busyAction is not null;
    public bool IsNotBusy => !IsBusy;
    public bool IsLoading => _busyAction == "load";
    public bool IsDriverEmployeesBusy { get => _driverEmployeesBusy; private set => SetField(ref _driverEmployeesBusy, value); }
    public string RefreshButtonText => IsLoading ? "Đang tải..." : "Tải lại";
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool HasTrips => Trips.Count > 0;
    public bool HasSelectedTrip => SelectedTrip is not null;
    public bool HasStops => Stops.Count > 0;
    public bool HasEligibleOrders => EligibleOrders.Count > 0;
    public bool HasAssignmentRoute => !string.IsNullOrWhiteSpace(AssignmentRouteId);
    public bool HasAssignmentTrip => !string.IsNullOrWhiteSpace(AssignmentTripId);

    public int ActiveTabIndex
    {
        get => _activeTabIndex;
        set => SetField(ref _activeTabIndex, Math.Clamp(value, 0, 1));
    }

    public int PlanningTabIndex
    {
        get => _planningTabIndex;
        set => SetField(ref _planningTabIndex, Math.Clamp(value, 0, 1));
    }

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

    public string RouteCode { get => _routeCode; set { if (SetField(ref _routeCode, value ?? string.Empty)) RaiseCreateActions(); } }
    public string RouteName { get => _routeName; set { if (SetField(ref _routeName, value ?? string.Empty)) RaiseCreateActions(); } }
    public string RouteDescription { get => _routeDescription; set => SetField(ref _routeDescription, value ?? string.Empty); }
    public string RouteWarehouseId { get => _routeWarehouseId; set { if (SetField(ref _routeWarehouseId, value ?? string.Empty)) RaiseCreateActions(); } }

    public string VehicleCode { get => _vehicleCode; set { if (SetField(ref _vehicleCode, value ?? string.Empty)) RaiseCreateActions(); } }
    public string VehiclePlate { get => _vehiclePlate; set { if (SetField(ref _vehiclePlate, value ?? string.Empty)) RaiseCreateActions(); } }
    public string VehicleType { get => _vehicleType; set { if (SetField(ref _vehicleType, value ?? string.Empty)) RaiseCreateActions(); } }

    public string DriverEmployeeId
    {
        get => _driverEmployeeId;
        set
        {
            if (!SetField(ref _driverEmployeeId, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(SelectedDriverEmployeeSummary));
            RaiseCreateActions();
        }
    }

    public string DriverLicenseReference { get => _driverLicenseReference; set => SetField(ref _driverLicenseReference, value ?? string.Empty); }

    public string SelectedDriverEmployeeSummary
    {
        get
        {
            var employee = _driverEmployees.FirstOrDefault(row => row.Id == DriverEmployeeId);
            if (employee is null) return "Mã, tên và số điện thoại tài xế được lấy từ hồ sơ nhân sự.";
            var phone = string.IsNullOrWhiteSpace(employee.Phone) ? string.Empty : $" · {employee.Phone}";
            return $"Mã, tên và số điện thoại lấy từ hồ sơ nhân sự: {employee.Code} · {employee.FullName}{phone}";
        }
    }

    public string TripWarehouseId
    {
        get => _tripWarehouseId;
        set
        {
            var next = value ?? string.Empty;
            if (!SetField(ref _tripWarehouseId, next)) return;
            if (!RouteMatchesWarehouse(TripRouteId, next)) _tripRouteId = string.Empty;
            OnPropertyChanged(nameof(TripRouteId));
            RebuildTripRouteOptions();
            RaiseCreateActions();
        }
    }

    public string TripRouteId { get => _tripRouteId; set => SetField(ref _tripRouteId, value ?? string.Empty); }
    public string TripVehicleId { get => _tripVehicleId; set => SetField(ref _tripVehicleId, value ?? string.Empty); }
    public string TripDriverId { get => _tripDriverId; set => SetField(ref _tripDriverId, value ?? string.Empty); }
    public string PlannedStartText { get => _plannedStartText; set => SetField(ref _plannedStartText, value ?? string.Empty); }
    public string TripNote { get => _tripNote; set => SetField(ref _tripNote, value ?? string.Empty); }

    public DeliveryTripListRow? SelectedTripRow
    {
        get => _selectedTripRow;
        set
        {
            if (!SetField(ref _selectedTripRow, value)) return;
            if (value is not null) _ = LoadTripAsync(value.Data.Id);
        }
    }

    public DeliveryTripData? SelectedTrip
    {
        get => _selectedTrip;
        private set
        {
            if (!SetField(ref _selectedTrip, value)) return;
            Replace(Stops, value is null ? [] : TripPlanningPresentation.Stops(value));
            if (value is not null) SeedTripDraft(value);
            RaiseSelectedTripState();
        }
    }

    public string SelectedTripNumber => SelectedTrip?.Number ?? "Chọn một chuyến";
    public string SelectedTripStatus => TripPlanningPresentation.Status(SelectedTrip?.Status);
    public string SelectedTripReadOnlyNotice => SelectedTrip?.Status switch
    {
        "planned" => "Kế hoạch đã lập và đang chỉ đọc. Mở lại chỉnh sửa để thay đổi xe, tài xế, điểm dừng hoặc phiếu giao.",
        "locked" => "Kế hoạch đã khóa. Xe, tài xế, điểm dừng và phiếu giao chỉ được đọc.",
        _ => string.Empty
    };
    public bool ShowReadOnlyNotice => !string.IsNullOrWhiteSpace(SelectedTripReadOnlyNotice);
    public bool IsDraftSelected => SelectedTrip?.Status == "draft";
    public bool ShowSaveAction => CanPlanTrip && IsDraftSelected;
    public bool ShowPlanAction => CanPlanTrip && IsDraftSelected && Stops.Count > 0;
    public bool ShowReopenAction => CanPlanTrip && SelectedTrip?.Status == "planned";
    public bool ShowLockAction => CanLockTrip && SelectedTrip?.Status == "planned";
    public bool CanSaveSelected => ShowSaveAction && IsNotBusy;
    public bool CanPlanSelected => ShowPlanAction && IsNotBusy;
    public bool CanReopenSelected => ShowReopenAction && IsNotBusy;
    public bool CanLockSelected => ShowLockAction && IsNotBusy;
    public bool ShowAssignmentEditActions => CanAssignTrip && IsDraftSelected;
    public bool CanEditAssignments => ShowAssignmentEditActions && IsNotBusy;

    public string AssignmentRouteId
    {
        get => _assignmentRouteId;
        set
        {
            if (!SetField(ref _assignmentRouteId, value ?? string.Empty)) return;
            _assignmentTripId = string.Empty;
            OnPropertyChanged(nameof(AssignmentTripId));
            RebuildAssignmentTrips();
            RebuildEligibleOrders();
            OnPropertyChanged(nameof(HasAssignmentRoute));
        }
    }

    public string AssignmentTripId
    {
        get => _assignmentTripId;
        set
        {
            if (!SetField(ref _assignmentTripId, value ?? string.Empty)) return;
            RebuildEligibleOrders();
            OnPropertyChanged(nameof(HasAssignmentTrip));
            OnPropertyChanged(nameof(AssignmentTripNumber));
            var row = Trips.FirstOrDefault(candidate => candidate.Data.Id == _assignmentTripId);
            if (row is not null)
            {
                _selectedTripRow = row;
                OnPropertyChanged(nameof(SelectedTripRow));
                _ = LoadTripAsync(row.Data.Id);
            }
            RaiseAssignmentState();
        }
    }

    public string AssignmentTripNumber =>
        _trips.FirstOrDefault(row => row.Id == AssignmentTripId)?.Number ?? string.Empty;
    public int SelectedEligibleCount => EligibleOrders.Count(row => row.IsSelected);
    public string AssignButtonText => $"Gán {SelectedEligibleCount} đơn";
    public bool CanChooseAssignmentTrip => HasAssignmentRoute && IsNotBusy;
    public bool CanAssignSelected => CanAssignTrip && IsNotBusy && HasAssignmentTrip && SelectedEligibleCount > 0;

    public bool CanCreateRoute =>
        CanManageRoutes && IsNotBusy
        && !string.IsNullOrWhiteSpace(RouteCode)
        && !string.IsNullOrWhiteSpace(RouteName)
        && !string.IsNullOrWhiteSpace(RouteWarehouseId);

    public bool CanCreateVehicle =>
        CanManageVehicles && IsNotBusy
        && !string.IsNullOrWhiteSpace(VehicleCode)
        && !string.IsNullOrWhiteSpace(VehiclePlate)
        && !string.IsNullOrWhiteSpace(VehicleType);

    public bool CanCreateDriver =>
        CanManageDrivers && IsNotBusy && !IsDriverEmployeesBusy && !string.IsNullOrWhiteSpace(DriverEmployeeId);

    public bool CanCreateTripNow => CanCreateTrip && IsNotBusy && !string.IsNullOrWhiteSpace(TripWarehouseId);

    public async Task<bool> EnsureLoadedAsync()
    {
        if (_loaded) return true;
        if (_initialLoadTask is not null) return await _initialLoadTask.ConfigureAwait(true);
        _initialLoadTask = LoadAllAsync();
        try
        {
            _loaded = await _initialLoadTask.ConfigureAwait(true);
            return _loaded;
        }
        finally { _initialLoadTask = null; }
    }

    public Task<bool> RefreshAsync() => LoadAllAsync(SelectedTrip?.Id);

    public async Task EnsureDriverEmployeesAsync(bool force = false)
    {
        if (!CanReadDrivers || IsDriverEmployeesBusy || (_driverEmployeesLoaded && !force)) return;
        IsDriverEmployeesBusy = true;
        try
        {
            var employees = await _service.ListDriverEmployeesAsync().ConfigureAwait(true);
            _driverEmployees.Clear();
            _driverEmployees.AddRange(employees.Where(row => row.IsActive));
            Replace(DriverEmployeeOptions, _driverEmployees.Select(TripPlanningPresentation.Employee));
            if (!_driverEmployees.Any(row => row.Id == DriverEmployeeId))
                DriverEmployeeId = string.Empty;
            _driverEmployeesLoaded = true;
        }
        catch (Exception exception) { SetError(exception); }
        finally
        {
            IsDriverEmployeesBusy = false;
            RaiseCreateActions();
        }
    }

    public async Task CreateRouteAsync()
    {
        if (!CanCreateRoute) return;
        var code = RouteCode.Trim();
        var name = RouteName.Trim();
        var description = RouteDescription.Trim();
        var warehouseId = RouteWarehouseId.Trim();
        if (code.Length > 64) { SetErrorMessage("Mã tuyến tối đa 64 ký tự."); return; }
        if (name.Length > 256) { SetErrorMessage("Tên tuyến tối đa 256 ký tự."); return; }

        var intent = Intent("create-route", code, name, description, warehouseId);
        await ExecuteMutationAsync(intent, async key =>
        {
            await _service.CreateRouteAsync(
                new LogisticsRouteCreateRequest(code, name, EmptyToNull(description), warehouseId),
                key).ConfigureAwait(true);
            RouteCode = RouteName = RouteDescription = RouteWarehouseId = string.Empty;
            await LoadAllAsync(preserveMessage: true).ConfigureAwait(true);
            SetNotice("Đã thêm tuyến giao.");
        }).ConfigureAwait(true);
    }

    public async Task CreateVehicleAsync()
    {
        if (!CanCreateVehicle) return;
        var code = VehicleCode.Trim();
        var plate = VehiclePlate.Trim();
        var type = VehicleType.Trim();
        if (code.Length > 64) { SetErrorMessage("Mã xe tối đa 64 ký tự."); return; }
        if (plate.Length > 32) { SetErrorMessage("Biển số tối đa 32 ký tự."); return; }
        if (type.Length > 80) { SetErrorMessage("Loại xe tối đa 80 ký tự."); return; }

        var intent = Intent("create-vehicle", code, plate, type);
        await ExecuteMutationAsync(intent, async key =>
        {
            await _service.CreateVehicleAsync(new LogisticsVehicleCreateRequest(code, plate, type), key).ConfigureAwait(true);
            VehicleCode = VehiclePlate = VehicleType = string.Empty;
            await LoadAllAsync(preserveMessage: true).ConfigureAwait(true);
            SetNotice("Đã thêm phương tiện.");
        }).ConfigureAwait(true);
    }

    public async Task CreateDriverAsync()
    {
        if (!CanCreateDriver) return;
        var employeeId = DriverEmployeeId.Trim();
        var license = DriverLicenseReference.Trim();
        if (license.Length > 128) { SetErrorMessage("Thông tin bằng lái tối đa 128 ký tự."); return; }
        var intent = Intent("create-driver", employeeId, license);

        await ExecuteMutationAsync(intent, async key =>
        {
            await _service.CreateDriverAsync(
                new LogisticsDriverCreateRequest(employeeId, EmptyToNull(license)),
                key).ConfigureAwait(true);
            DriverEmployeeId = DriverLicenseReference = string.Empty;
            _driverEmployeesLoaded = false;
            await Task.WhenAll(
                LoadAllAsync(preserveMessage: true),
                EnsureDriverEmployeesAsync(true)).ConfigureAwait(true);
            SetNotice("Đã liên kết nhân sự với hồ sơ tài xế.");
        }).ConfigureAwait(true);
    }

    public async Task CreateTripAsync()
    {
        if (!CanCreateTripNow) return;
        if (!ValidateTripDraft(out var plannedStartAt)) return;
        var intent = Intent(
            "create-trip",
            TripWarehouseId, TripRouteId, TripVehicleId, TripDriverId, plannedStartAt, TripNote.Trim());

        await ExecuteMutationAsync(intent, async key =>
        {
            var result = await _service.CreateTripAsync(new DeliveryTripCreateRequest
            {
                WarehouseId = TripWarehouseId,
                DeliveryRouteId = EmptyToNull(TripRouteId),
                VehicleId = EmptyToNull(TripVehicleId),
                PrimaryDriverId = EmptyToNull(TripDriverId),
                PlannedStartAt = plannedStartAt,
                Note = EmptyToNull(TripNote)
            }, key).ConfigureAwait(true);
            await LoadAllAsync(result.Trip.Id, preserveMessage: true).ConfigureAwait(true);
            PlanningTabIndex = 1;
            SetNotice($"Đã tạo chuyến {result.Trip.Number}.");
        }).ConfigureAwait(true);
    }

    public async Task UpdateTripAsync()
    {
        var trip = SelectedTrip;
        if (trip is null || !CanSaveSelected || !ValidateTripDraft(out var plannedStartAt)) return;
        var intent = Intent(
            "update-trip", trip.Id, trip.Revision, TripRouteId, TripVehicleId, TripDriverId, plannedStartAt, TripNote.Trim());

        await ExecuteMutationAsync(intent, async key =>
        {
            var result = await _service.UpdateTripAsync(trip.Id, new DeliveryTripUpdateRequest
            {
                DeliveryRouteId = EmptyToNull(TripRouteId),
                VehicleId = EmptyToNull(TripVehicleId),
                PrimaryDriverId = EmptyToNull(TripDriverId),
                PlannedStartAt = plannedStartAt,
                Note = EmptyToNull(TripNote)
            }, key).ConfigureAwait(true);
            await LoadAllAsync(result.Trip.Id, preserveMessage: true).ConfigureAwait(true);
            SetNotice("Đã cập nhật kế hoạch chuyến.");
        }).ConfigureAwait(true);
    }

    public async Task AssignSelectedAsync()
    {
        var trip = _trips.FirstOrDefault(row => row.Id == AssignmentTripId && row.Status == "draft");
        var ids = EligibleOrders.Where(row => row.IsSelected && row.CanSelect)
            .Select(row => row.Data.Id)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        if (trip is null || ids.Length == 0 || !CanAssignTrip) return;

        var intent = Intent("assign-batch", trip.Id, string.Join(".", ids));
        await ExecuteMutationAsync(intent, async key =>
        {
            var result = await _service.AssignAsync(trip.Id, new DeliveryTripAssignRequest(ids), key).ConfigureAwait(true);
            await LoadAllAsync(result.Trip.Id, preserveMessage: true).ConfigureAwait(true);
            AssignmentTripId = trip.Id;
            var count = result.AssignmentCount ?? ids.Length;
            SetNotice($"Đã gán {count} phiếu giao vào chuyến {result.Trip.Number}.");
        }).ConfigureAwait(true);
    }

    public async Task UnassignAsync(TripAssignmentRow? row)
    {
        var trip = SelectedTrip;
        if (trip is null || row is null || !CanEditAssignments) return;
        var intent = Intent("unassign", trip.Id, trip.Revision, row.Data.DeliveryOrderId);
        await ExecuteMutationAsync(intent, async key =>
        {
            var result = await _service.UnassignAsync(
                trip.Id,
                new DeliveryTripUnassignRequest(row.Data.DeliveryOrderId, "Điều chỉnh kế hoạch chuyến"),
                key).ConfigureAwait(true);
            await LoadAllAsync(result.Trip.Id, preserveMessage: true).ConfigureAwait(true);
            SetNotice("Đã bỏ phiếu giao khỏi chuyến.");
        }).ConfigureAwait(true);
    }

    public async Task MoveStopAsync(TripStopRow? row, int direction)
    {
        var trip = SelectedTrip;
        if (trip is null || row is null || !CanEditAssignments || direction is not (-1 or 1)) return;
        var ordered = Stops.Select(stop => stop.Data.Id).ToList();
        var index = ordered.IndexOf(row.Data.Id);
        var target = index + direction;
        if (index < 0 || target < 0 || target >= ordered.Count) return;
        (ordered[index], ordered[target]) = (ordered[target], ordered[index]);
        var ids = ordered.ToArray();
        var intent = Intent("reorder", trip.Id, trip.Revision, string.Join(".", ids));

        await ExecuteMutationAsync(intent, async key =>
        {
            var result = await _service.ReorderAsync(trip.Id, new DeliveryTripReorderRequest(ids), key).ConfigureAwait(true);
            await LoadAllAsync(result.Trip.Id, preserveMessage: true).ConfigureAwait(true);
            SetNotice("Đã cập nhật thứ tự điểm giao.");
        }).ConfigureAwait(true);
    }

    public Task PlanAsync() => ExecuteTripActionAsync("plan");
    public Task ReopenAsync() => ExecuteTripActionAsync("reopen");
    public Task LockAsync() => ExecuteTripActionAsync("lock");

    public void NotifyEligibleSelectionChanged() => RaiseAssignmentState();

    private async Task ExecuteTripActionAsync(string action)
    {
        var trip = SelectedTrip;
        if (trip is null) return;
        if (action == "plan" && !CanPlanSelected) return;
        if (action == "reopen" && !CanReopenSelected) return;
        if (action == "lock" && !CanLockSelected) return;

        var intent = Intent(action, trip.Id, trip.Revision);
        await ExecuteMutationAsync(intent, async key =>
        {
            TripMutationData result = action switch
            {
                "plan" => await _service.PlanAsync(trip.Id, key).ConfigureAwait(true),
                "reopen" => await _service.ReopenAsync(
                    trip.Id,
                    new DeliveryTripReopenRequest("Điều chỉnh kế hoạch trước khi khóa"),
                    key).ConfigureAwait(true),
                "lock" => await _service.LockAsync(trip.Id, key).ConfigureAwait(true),
                _ => throw new InvalidOperationException("Thao tác chuyến không hợp lệ.")
            };
            await LoadAllAsync(result.Trip.Id, preserveMessage: true).ConfigureAwait(true);
            SetNotice(action switch
            {
                "plan" => "Chuyến đã chuyển sang trạng thái lập kế hoạch.",
                "reopen" => "Chuyến đã mở lại để chỉnh sửa.",
                _ => "Kế hoạch chuyến đã khóa."
            });
        }).ConfigureAwait(true);
    }

    private async Task ExecuteMutationAsync(string intent, Func<string, Task> operation)
    {
        if (IsBusy) return;
        SetBusy(intent);
        ClearMessage();
        try
        {
            await operation(KeyFor(intent)).ConfigureAwait(true);
            _intentKeys.Remove(intent);
        }
        catch (Exception exception)
        {
            ApplyServerFieldError(exception);
            SetError(exception);
        }
        finally { SetBusy(null); }
    }

    private async Task<bool> LoadAllAsync(string? preferredTripId = null, bool preserveMessage = false)
    {
        if (!CanRead)
        {
            _loaded = false;
            if (_access.Current.IsAuthenticated) SetErrorMessage(ReadDenied);
            return false;
        }

        var generation = _accessGeneration;
        var request = ++_loadGeneration;
        var existingTripId = preferredTripId ?? SelectedTrip?.Id ?? SelectedTripRow?.Data.Id;
        SetBusy("load");
        if (!preserveMessage) ClearMessage();

        try
        {
            var warehousesTask = _service.ListWarehousesAsync();
            var routesTask = _service.ListRoutesAsync();
            var vehiclesTask = _service.ListVehiclesAsync();
            var driversTask = _service.ListDriversAsync();
            var eligibleTask = _service.ListEligibleDeliveryOrdersAsync();
            var tripsTask = _service.ListTripsAsync();
            await Task.WhenAll(warehousesTask, routesTask, vehiclesTask, driversTask, eligibleTask, tripsTask).ConfigureAwait(true);

            if (generation != _accessGeneration || request != _loadGeneration || !CanRead) return false;

            _warehouses.Clear(); _warehouses.AddRange(await warehousesTask.ConfigureAwait(true));
            _routes.Clear(); _routes.AddRange(await routesTask.ConfigureAwait(true));
            _vehicles.Clear(); _vehicles.AddRange(await vehiclesTask.ConfigureAwait(true));
            _drivers.Clear(); _drivers.AddRange(await driversTask.ConfigureAwait(true));
            _eligible.Clear(); _eligible.AddRange(await eligibleTask.ConfigureAwait(true));
            _trips.Clear(); _trips.AddRange(await tripsTask.ConfigureAwait(true));

            Replace(WarehouseOptions, _warehouses.Select(TripPlanningPresentation.Warehouse));
            Replace(RouteWarehouseOptions, _warehouses.Select(TripPlanningPresentation.Warehouse));
            Replace(VehicleOptions, _vehicles.Where(row => row.IsActive && row.OperationalStatus == "AVAILABLE").Select(TripPlanningPresentation.Vehicle));
            Replace(DriverOptions, _drivers.Where(row => row.IsActive).Select(TripPlanningPresentation.Driver));
            Replace(AssignmentRouteOptions, _routes.Where(row => row.IsActive && !string.IsNullOrWhiteSpace(row.DefaultWarehouseId)).Select(row => TripPlanningPresentation.Route(row, true)));
            Replace(Trips, _trips.Select(TripPlanningPresentation.Trip));

            if (!_warehouses.Any(row => row.Id == TripWarehouseId))
                _tripWarehouseId = _warehouses.FirstOrDefault()?.Id ?? string.Empty;
            if (!_warehouses.Any(row => row.Id == RouteWarehouseId))
                _routeWarehouseId = string.Empty;
            if (!_vehicles.Any(row => row.Id == TripVehicleId && row.IsActive && row.OperationalStatus == "AVAILABLE"))
                _tripVehicleId = string.Empty;
            if (!_drivers.Any(row => row.Id == TripDriverId && row.IsActive))
                _tripDriverId = string.Empty;
            if (!RouteMatchesWarehouse(TripRouteId, TripWarehouseId))
                _tripRouteId = string.Empty;

            OnPropertyChanged(nameof(TripWarehouseId));
            OnPropertyChanged(nameof(RouteWarehouseId));
            OnPropertyChanged(nameof(TripVehicleId));
            OnPropertyChanged(nameof(TripDriverId));
            OnPropertyChanged(nameof(TripRouteId));
            RebuildTripRouteOptions();
            RebuildAssignmentTrips();

            var targetRow = string.IsNullOrWhiteSpace(existingTripId)
                ? null
                : Trips.FirstOrDefault(row => row.Data.Id == existingTripId);
            _selectedTripRow = targetRow;
            OnPropertyChanged(nameof(SelectedTripRow));

            if (targetRow is not null)
                await LoadTripAsync(targetRow.Data.Id, request).ConfigureAwait(true);
            else
                SelectedTrip = null;

            _loaded = true;
            RebuildEligibleOrders();
            OnPropertyChanged(nameof(HasTrips));
            RaiseCreateActions();
            RaiseAssignmentState();
            return true;
        }
        catch (Exception exception)
        {
            if (generation == _accessGeneration && request == _loadGeneration) SetError(exception);
            return false;
        }
        finally
        {
            if (request == _loadGeneration && _busyAction == "load") SetBusy(null);
        }
    }

    private async Task LoadTripAsync(string tripId, long? parentRequest = null)
    {
        var generation = _accessGeneration;
        var request = parentRequest ?? ++_loadGeneration;
        var ownsBusy = parentRequest is null;
        if (ownsBusy) SetBusy($"detail-{tripId}");
        try
        {
            var detail = await _service.GetTripAsync(tripId).ConfigureAwait(true);
            if (generation != _accessGeneration || request != _loadGeneration || !CanRead) return;
            SelectedTrip = detail;
        }
        catch (Exception exception)
        {
            if (generation == _accessGeneration && request == _loadGeneration) SetError(exception);
        }
        finally
        {
            if (ownsBusy && request == _loadGeneration && _busyAction == $"detail-{tripId}") SetBusy(null);
        }
    }

    private void SeedTripDraft(DeliveryTripData trip)
    {
        _tripWarehouseId = trip.WarehouseId;
        _tripRouteId = trip.DeliveryRouteId ?? string.Empty;
        _tripVehicleId = trip.VehicleId ?? string.Empty;
        _tripDriverId = trip.PrimaryDriverId ?? string.Empty;
        _plannedStartText = TripPlanningPresentation.LocalDateTime(trip.PlannedStartAt);
        _tripNote = trip.Note ?? string.Empty;
        RebuildTripRouteOptions();
        OnPropertyChanged(nameof(TripWarehouseId));
        OnPropertyChanged(nameof(TripRouteId));
        OnPropertyChanged(nameof(TripVehicleId));
        OnPropertyChanged(nameof(TripDriverId));
        OnPropertyChanged(nameof(PlannedStartText));
        OnPropertyChanged(nameof(TripNote));
    }

    private bool ValidateTripDraft(out string? plannedStartAt)
    {
        plannedStartAt = null;
        if (string.IsNullOrWhiteSpace(TripWarehouseId) || !_warehouses.Any(row => row.Id == TripWarehouseId))
        {
            SetErrorMessage("Chọn kho xuất phát hợp lệ.");
            return false;
        }
        if (!string.IsNullOrWhiteSpace(TripRouteId) && !RouteMatchesWarehouse(TripRouteId, TripWarehouseId))
        {
            SetErrorMessage("Tuyến giao không thuộc kho xuất phát đã chọn.");
            return false;
        }
        if (!string.IsNullOrWhiteSpace(TripVehicleId) &&
            !_vehicles.Any(row => row.Id == TripVehicleId && row.IsActive && row.OperationalStatus == "AVAILABLE"))
        {
            SetErrorMessage("Phương tiện không còn khả dụng.");
            return false;
        }
        if (!string.IsNullOrWhiteSpace(TripDriverId) && !_drivers.Any(row => row.Id == TripDriverId && row.IsActive))
        {
            SetErrorMessage("Tài xế không còn khả dụng.");
            return false;
        }
        if (!TripPlanningPresentation.TryIsoDateTime(PlannedStartText, out plannedStartAt))
        {
            SetErrorMessage("Giờ dự kiến phải theo định dạng yyyy-MM-dd HH:mm.");
            return false;
        }
        if (TripNote.Trim().Length > 4000)
        {
            SetErrorMessage("Ghi chú tối đa 4.000 ký tự.");
            return false;
        }
        return true;
    }

    private void RebuildTripRouteOptions() =>
        Replace(TripRouteOptions, _routes
            .Where(row => row.IsActive && row.DefaultWarehouseId == TripWarehouseId)
            .Select(row => TripPlanningPresentation.Route(row)));

    private void RebuildAssignmentTrips()
    {
        Replace(AssignmentTripOptions, _trips
            .Where(row => row.Status == "draft" && row.DeliveryRouteId == AssignmentRouteId)
            .Select(row => new TripOption(row.Id, $"{row.Number} · {TripPlanningPresentation.Status(row.Status)}")));

        if (!AssignmentTripOptions.Any(row => row.Id == _assignmentTripId))
        {
            _assignmentTripId = string.Empty;
            OnPropertyChanged(nameof(AssignmentTripId));
        }
    }

    private void RebuildEligibleOrders()
    {
        var trip = _trips.FirstOrDefault(row => row.Id == AssignmentTripId && row.Status == "draft");
        Replace(EligibleOrders, trip is null
            ? []
            : _eligible.Where(row => row.WarehouseId == trip.WarehouseId).Select(row => new EligibleDeliveryOrderRow(row)));
        OnPropertyChanged(nameof(HasEligibleOrders));
        RaiseAssignmentState();
    }

    private bool RouteMatchesWarehouse(string routeId, string warehouseId) =>
        string.IsNullOrWhiteSpace(routeId)
        || _routes.Any(row => row.Id == routeId && row.IsActive && row.DefaultWarehouseId == warehouseId);

    private string Intent(string prefix, params string?[] parts) =>
        $"{prefix}|{string.Join("|", parts.Select(value => value?.Trim() ?? string.Empty))}";

    private string KeyFor(string intent)
    {
        if (_intentKeys.TryGetValue(intent, out var existing)) return existing;
        if (_intentKeys.Count >= IntentCacheLimit)
        {
            var oldest = _intentKeys.Keys.FirstOrDefault();
            if (oldest is not null) _intentKeys.Remove(oldest);
        }
        var prefix = intent.Split('|', 2)[0];
        var key = _idempotencyKeys.Create($"trip-planning-{prefix}");
        _intentKeys[intent] = key;
        return key;
    }

    private void ApplyServerFieldError(Exception exception)
    {
        if (exception is not CanonicalApiException api) return;
        var message = api.Code switch
        {
            "INVALID_LOGISTICS_ROUTE" or "DELIVERY_ROUTE_WAREHOUSE_NOT_AVAILABLE" => "Chọn kho áp dụng hợp lệ cho tuyến.",
            "INVALID_VEHICLE" => "Kiểm tra lại mã xe, biển số và loại xe.",
            "INVALID_DRIVER_PROFILE" => "Bắt buộc chọn nhân sự hợp lệ cho tài xế.",
            "DRIVER_EMPLOYEE_NOT_AVAILABLE" => "Nhân sự không còn hoạt động hoặc không thuộc đơn vị hiện tại.",
            "DRIVER_EMPLOYEE_ALREADY_LINKED" => "Nhân sự này đã có hồ sơ tài xế đang hoạt động.",
            "DELIVERY_ROUTE_WAREHOUSE_MISMATCH" or "DELIVERY_ROUTE_NOT_AVAILABLE" => "Tuyến không thuộc kho xuất phát đã chọn.",
            "INVALID_DELIVERY_TRIP" => "Kiểm tra kho xuất phát và giờ dự kiến.",
            _ => null
        };
        if (message is not null) SetErrorMessage(message);
    }

    private void Reset()
    {
        _warehouses.Clear();
        _routes.Clear();
        _vehicles.Clear();
        _drivers.Clear();
        _driverEmployees.Clear();
        _eligible.Clear();
        _trips.Clear();
        WarehouseOptions.Clear();
        RouteWarehouseOptions.Clear();
        TripRouteOptions.Clear();
        VehicleOptions.Clear();
        DriverOptions.Clear();
        DriverEmployeeOptions.Clear();
        Trips.Clear();
        Stops.Clear();
        AssignmentRouteOptions.Clear();
        AssignmentTripOptions.Clear();
        EligibleOrders.Clear();
        _selectedTripRow = null;
        _selectedTrip = null;
        _assignmentRouteId = string.Empty;
        _assignmentTripId = string.Empty;
        _tripWarehouseId = string.Empty;
        _tripRouteId = string.Empty;
        _tripVehicleId = string.Empty;
        _tripDriverId = string.Empty;
        _plannedStartText = string.Empty;
        _tripNote = string.Empty;
        ClearMessage();
        OnPropertyChanged(nameof(SelectedTripRow));
        OnPropertyChanged(nameof(AssignmentRouteId));
        OnPropertyChanged(nameof(AssignmentTripId));
        RaiseSelectedTripState();
        RaiseAssignmentState();
    }

    private void SetBusy(string? action)
    {
        if (string.Equals(_busyAction, action, StringComparison.Ordinal)) return;
        _busyAction = action;
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsNotBusy));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(RefreshButtonText));
        RaiseCreateActions();
        RaiseSelectedTripState();
        RaiseAssignmentState();
    }

    private void RaiseCreateActions()
    {
        OnPropertyChanged(nameof(CanCreateRoute));
        OnPropertyChanged(nameof(CanCreateVehicle));
        OnPropertyChanged(nameof(CanCreateDriver));
        OnPropertyChanged(nameof(CanCreateTripNow));
    }

    private void RaiseSelectedTripState()
    {
        OnPropertyChanged(nameof(HasSelectedTrip));
        OnPropertyChanged(nameof(HasStops));
        OnPropertyChanged(nameof(SelectedTripNumber));
        OnPropertyChanged(nameof(SelectedTripStatus));
        OnPropertyChanged(nameof(SelectedTripReadOnlyNotice));
        OnPropertyChanged(nameof(ShowReadOnlyNotice));
        OnPropertyChanged(nameof(IsDraftSelected));
        OnPropertyChanged(nameof(ShowSaveAction));
        OnPropertyChanged(nameof(ShowPlanAction));
        OnPropertyChanged(nameof(ShowReopenAction));
        OnPropertyChanged(nameof(ShowLockAction));
        OnPropertyChanged(nameof(CanSaveSelected));
        OnPropertyChanged(nameof(CanPlanSelected));
        OnPropertyChanged(nameof(CanReopenSelected));
        OnPropertyChanged(nameof(CanLockSelected));
        OnPropertyChanged(nameof(ShowAssignmentEditActions));
        OnPropertyChanged(nameof(CanEditAssignments));
    }

    private void RaiseAssignmentState()
    {
        OnPropertyChanged(nameof(HasAssignmentRoute));
        OnPropertyChanged(nameof(HasAssignmentTrip));
        OnPropertyChanged(nameof(AssignmentTripNumber));
        OnPropertyChanged(nameof(CanChooseAssignmentTrip));
        OnPropertyChanged(nameof(SelectedEligibleCount));
        OnPropertyChanged(nameof(AssignButtonText));
        OnPropertyChanged(nameof(CanAssignSelected));
    }

    private void RaisePermissions()
    {
        OnPropertyChanged(nameof(CanRead));
        OnPropertyChanged(nameof(CanManageRoutes));
        OnPropertyChanged(nameof(CanManageVehicles));
        OnPropertyChanged(nameof(CanReadDrivers));
        OnPropertyChanged(nameof(CanManageDrivers));
        OnPropertyChanged(nameof(CanCreateTrip));
        OnPropertyChanged(nameof(CanPlanTrip));
        OnPropertyChanged(nameof(CanAssignTrip));
        OnPropertyChanged(nameof(CanLockTrip));
        RaiseCreateActions();
        RaiseSelectedTripState();
        RaiseAssignmentState();
    }

    private void ClearMessage()
    {
        MessageIsError = false;
        Message = string.Empty;
    }

    private void SetNotice(string value)
    {
        MessageIsError = false;
        Message = value;
    }

    private void SetErrorMessage(string value)
    {
        MessageIsError = true;
        Message = value;
    }

    private void SetError(Exception exception)
    {
        if (MessageIsError && !string.IsNullOrWhiteSpace(Message)) return;
        SetErrorMessage(exception is CanonicalApiException api
            ? CanonicalErrorMessages.WithRequestId(CanonicalErrorMessages.ToOfficeMessage(api), api.RequestId)
            : exception.Message);
    }

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
