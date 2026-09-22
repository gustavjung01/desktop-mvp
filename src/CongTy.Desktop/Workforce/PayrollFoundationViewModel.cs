using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Workforce;

public sealed record PayrollChoice(string Value, string Label)
{
    public override string ToString() => Label;
}

public sealed partial class PayrollFoundationViewModel : INotifyPropertyChanged
{
    private const string PayrollReadPermission = "core.payroll.read";
    private const string PayrollManagePermission = "core.payroll.manage";
    private const string PayrollClosePermission = "core.payroll.close";
    private const string PayrollAdjustPermission = "core.payroll.adjust";
    private const string PayrollExportPermission = "core.payroll.export";
    private static readonly Regex MoneyPattern = new(@"^\d{1,15}(?:\.\d{1,2})?$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex ComponentCodePattern = new(@"^[A-Z0-9._-]{2,32}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IPayrollFoundationService _service;
    private readonly ICanonicalIdempotencyKeyProvider _idempotencyKeys;
    private readonly IAccessStateService _access;
    private readonly Dictionary<string, string> _mutationKeys = new(StringComparer.Ordinal);

    private bool _loaded;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;
    private PayrollCapabilitiesData _capabilities = new();
    private PayrollPeriodData? _selectedPeriod;

    private PayrollAttendanceSourceOption? _selectedAttendanceSource;
    private PayrollEmployeeOption? _salaryEmployee;
    private string _salaryAmount = string.Empty;
    private DateTime? _salaryEffectiveDate;
    private string _salaryNote = string.Empty;

    private PayrollEmployeeOption? _fixedEmployee;
    private PayrollComponentOption? _fixedComponent;
    private string _fixedAmount = string.Empty;
    private DateTime? _fixedEffectiveDate;
    private string _fixedNote = string.Empty;

    private string _componentCode = string.Empty;
    private string _componentName = string.Empty;
    private PayrollChoice? _componentCategory;
    private PayrollChoice? _componentRecurrence;
    private PayrollChoice? _componentInputMode;
    private bool _componentProrate;
    private bool _componentIncludeGross = true;
    private bool _componentIncludeNet = true;
    private DateTime? _componentEffectiveDate;

    private PayrollEmployeeOption? _periodEmployee;
    private PayrollComponentOption? _periodComponent;
    private string _periodAmount = string.Empty;
    private string _periodNote = string.Empty;

    public PayrollFoundationViewModel(
        IPayrollFoundationService service,
        ICanonicalIdempotencyKeyProvider idempotencyKeys,
        IAccessStateService access)
    {
        _service = service;
        _idempotencyKeys = idempotencyKeys;
        _access = access;

        CategoryOptions =
        [
            new PayrollChoice("INCOME", "Thu nhập lương"),
            new PayrollChoice("DEDUCTION", "Khấu trừ"),
            new PayrollChoice("REIMBURSEMENT", "Hoàn chi phí")
        ];
        RecurrenceOptions =
        [
            new PayrollChoice("FIXED", "Cố định"),
            new PayrollChoice("PERIOD", "Theo kỳ")
        ];
        InputModeOptions =
        [
            new PayrollChoice("AUTOMATIC", "Tự động"),
            new PayrollChoice("MANUAL", "Nhập tay")
        ];
        ComponentCategory = CategoryOptions[0];
        ComponentRecurrence = RecurrenceOptions[1];
        ComponentInputMode = InputModeOptions[1];

        var today = WorkSchedulePresentation.BusinessToday();
        SalaryEffectiveDate = today;
        FixedEffectiveDate = today;
        ComponentEffectiveDate = today;

        _access.Changed += (_, _) => RunOnUiThread(ResetForAccessChange);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<PayrollAttendanceSourceOption> AttendanceSources { get; } = [];
    public ObservableCollection<PayrollPeriodRowView> PeriodRows { get; } = [];
    public ObservableCollection<PayrollEmployeeOption> Employees { get; } = [];
    public ObservableCollection<PayrollEmployeeOption> PeriodEmployees { get; } = [];
    public ObservableCollection<PayrollComponentOption> FixedComponentOptions { get; } = [];
    public ObservableCollection<PayrollComponentOption> PeriodComponentOptions { get; } = [];
    public ObservableCollection<PayrollSalaryRowView> SalaryRows { get; } = [];
    public ObservableCollection<PayrollFixedComponentRowView> FixedRows { get; } = [];
    public ObservableCollection<PayrollComponentTypeRowView> ComponentTypeRows { get; } = [];
    public ObservableCollection<PayrollPeriodComponentRowView> PeriodComponentRows { get; } = [];

    public IReadOnlyList<PayrollChoice> CategoryOptions { get; }
    public IReadOnlyList<PayrollChoice> RecurrenceOptions { get; }
    public IReadOnlyList<PayrollChoice> InputModeOptions { get; }

    public bool HasReadAccess =>
        _access.HasPermission(PayrollReadPermission)
        || _access.HasPermission(PayrollManagePermission)
        || _access.HasPermission(PayrollClosePermission)
        || _access.HasPermission(PayrollAdjustPermission)
        || _access.HasPermission(PayrollExportPermission);

    public bool CanManage =>
        _capabilities.CanManage
        && _access.HasPermission(PayrollManagePermission);

    public bool CanRefresh => HasReadAccess && !IsBusy;
    public bool CanCreatePeriod => CanManage && SelectedAttendanceSource is not null && !IsBusy;
    public bool CanSaveSalary => CanManage && SalaryEmployee is not null && SalaryEffectiveDate is not null && IsMoney(SalaryAmount) && !IsBusy;
    public bool CanSaveFixed => CanManage && FixedEmployee is not null && FixedComponent is not null && FixedEffectiveDate is not null && IsMoney(FixedAmount) && !IsBusy;
    public bool CanCreateComponent =>
        CanManage
        && ComponentEffectiveDate is not null
        && ComponentCodePattern.IsMatch(ComponentCode.Trim().ToUpperInvariant())
        && ComponentName.Trim().Length is >= 1 and <= 120
        && ComponentCategory is not null
        && ComponentRecurrence is not null
        && ComponentInputMode is not null
        && !IsBusy;
    public bool CanAddPeriodComponent =>
        CanManage
        && SelectedPeriod is not null
        && SelectedPeriod.Status != "CLOSED"
        && PeriodEmployee is not null
        && PeriodComponent is not null
        && IsMoney(PeriodAmount)
        && !string.IsNullOrWhiteSpace(PeriodNote)
        && !IsBusy;

    public bool ShowLoading => IsBusy && PeriodRows.Count == 0 && SalaryRows.Count == 0 && ComponentTypeRows.Count == 0;
    public bool IsPeriodEmpty => !IsBusy && PeriodRows.Count == 0;
    public bool IsSalaryEmpty => !IsBusy && SalaryRows.Count == 0;
    public bool IsFixedEmpty => !IsBusy && FixedRows.Count == 0;
    public bool IsComponentTypeEmpty => !IsBusy && ComponentTypeRows.Count == 0;
    public bool IsPeriodComponentEmpty => !IsBusy && PeriodComponentRows.Count == 0;
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool HasSelectedPeriod => SelectedPeriod is not null;

    public string PeriodCountText => $"{PeriodRows.Count} kỳ lương";
    public string AttendanceSourceCountText => $"{AttendanceSources.Count} kỳ công đã chốt";
    public string EmployeeCountText => $"{Employees.Count} nhân sự";
    public string ComponentCountText => $"{ComponentTypeRows.Count} loại khoản";

    public string SelectedPeriodTitle => SelectedPeriod is null
        ? "Chưa chọn kỳ lương"
        : PayrollFoundationPresentation.PeriodText(SelectedPeriod.PeriodStart, SelectedPeriod.PeriodEnd, SelectedPeriod.BranchName);
    public string SelectedPeriodSourceText => SelectedPeriod is null
        ? "Chọn một kỳ lương để xem nguồn dữ liệu."
        : $"Nguồn: bản chốt kỳ công lần {SelectedPeriod.AttendanceRevision}";
    public string SelectedPeriodStatusText => SelectedPeriod is null
        ? "Chưa tạo kỳ"
        : PayrollFoundationPresentation.PeriodStatusLabel(SelectedPeriod.Status);

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetField(ref _isBusy, value)) return;
            RaiseAvailability();
            RaiseEmptyStates();
        }
    }

    public string Message
    {
        get => _message;
        private set
        {
            if (!SetField(ref _message, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(HasMessage));
        }
    }

    public bool MessageIsError
    {
        get => _messageIsError;
        private set => SetField(ref _messageIsError, value);
    }

    public PayrollPeriodData? SelectedPeriod
    {
        get => _selectedPeriod;
        private set
        {
            if (!SetField(ref _selectedPeriod, value)) return;
            OnPropertyChanged(nameof(HasSelectedPeriod));
            OnPropertyChanged(nameof(SelectedPeriodTitle));
            OnPropertyChanged(nameof(SelectedPeriodSourceText));
            OnPropertyChanged(nameof(SelectedPeriodStatusText));
            OnPropertyChanged(nameof(CanAddPeriodComponent));
        }
    }

    public PayrollAttendanceSourceOption? SelectedAttendanceSource
    {
        get => _selectedAttendanceSource;
        set
        {
            if (!SetField(ref _selectedAttendanceSource, value)) return;
            OnPropertyChanged(nameof(CanCreatePeriod));
        }
    }

    public PayrollEmployeeOption? SalaryEmployee
    {
        get => _salaryEmployee;
        set
        {
            if (!SetField(ref _salaryEmployee, value)) return;
            OnPropertyChanged(nameof(CanSaveSalary));
        }
    }

    public string SalaryAmount
    {
        get => _salaryAmount;
        set
        {
            if (!SetField(ref _salaryAmount, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSaveSalary));
        }
    }

    public DateTime? SalaryEffectiveDate
    {
        get => _salaryEffectiveDate;
        set
        {
            if (!SetField(ref _salaryEffectiveDate, value)) return;
            OnPropertyChanged(nameof(CanSaveSalary));
        }
    }

    public string SalaryNote
    {
        get => _salaryNote;
        set => SetField(ref _salaryNote, value ?? string.Empty);
    }

    public PayrollEmployeeOption? FixedEmployee
    {
        get => _fixedEmployee;
        set
        {
            if (!SetField(ref _fixedEmployee, value)) return;
            OnPropertyChanged(nameof(CanSaveFixed));
        }
    }

    public PayrollComponentOption? FixedComponent
    {
        get => _fixedComponent;
        set
        {
            if (!SetField(ref _fixedComponent, value)) return;
            OnPropertyChanged(nameof(CanSaveFixed));
        }
    }

    public string FixedAmount
    {
        get => _fixedAmount;
        set
        {
            if (!SetField(ref _fixedAmount, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanSaveFixed));
        }
    }

    public DateTime? FixedEffectiveDate
    {
        get => _fixedEffectiveDate;
        set
        {
            if (!SetField(ref _fixedEffectiveDate, value)) return;
            OnPropertyChanged(nameof(CanSaveFixed));
        }
    }

    public string FixedNote
    {
        get => _fixedNote;
        set => SetField(ref _fixedNote, value ?? string.Empty);
    }

    public string ComponentCode
    {
        get => _componentCode;
        set
        {
            if (!SetField(ref _componentCode, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanCreateComponent));
        }
    }

    public string ComponentName
    {
        get => _componentName;
        set
        {
            if (!SetField(ref _componentName, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanCreateComponent));
        }
    }

    public PayrollChoice? ComponentCategory
    {
        get => _componentCategory;
        set
        {
            if (!SetField(ref _componentCategory, value)) return;
            if (value?.Value == "INCOME")
            {
                ComponentIncludeGross = true;
                ComponentIncludeNet = true;
            }
            else if (value is not null)
            {
                ComponentIncludeGross = false;
                ComponentIncludeNet = true;
            }
            OnPropertyChanged(nameof(CanCreateComponent));
        }
    }

    public PayrollChoice? ComponentRecurrence
    {
        get => _componentRecurrence;
        set
        {
            if (!SetField(ref _componentRecurrence, value)) return;
            OnPropertyChanged(nameof(CanCreateComponent));
        }
    }

    public PayrollChoice? ComponentInputMode
    {
        get => _componentInputMode;
        set
        {
            if (!SetField(ref _componentInputMode, value)) return;
            OnPropertyChanged(nameof(CanCreateComponent));
        }
    }

    public bool ComponentProrate
    {
        get => _componentProrate;
        set => SetField(ref _componentProrate, value);
    }

    public bool ComponentIncludeGross
    {
        get => _componentIncludeGross;
        set => SetField(ref _componentIncludeGross, value);
    }

    public bool ComponentIncludeNet
    {
        get => _componentIncludeNet;
        set => SetField(ref _componentIncludeNet, value);
    }

    public DateTime? ComponentEffectiveDate
    {
        get => _componentEffectiveDate;
        set
        {
            if (!SetField(ref _componentEffectiveDate, value)) return;
            OnPropertyChanged(nameof(CanCreateComponent));
        }
    }

    public PayrollEmployeeOption? PeriodEmployee
    {
        get => _periodEmployee;
        set
        {
            if (!SetField(ref _periodEmployee, value)) return;
            OnPropertyChanged(nameof(CanAddPeriodComponent));
        }
    }

    public PayrollComponentOption? PeriodComponent
    {
        get => _periodComponent;
        set
        {
            if (!SetField(ref _periodComponent, value)) return;
            OnPropertyChanged(nameof(CanAddPeriodComponent));
        }
    }

    public string PeriodAmount
    {
        get => _periodAmount;
        set
        {
            if (!SetField(ref _periodAmount, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanAddPeriodComponent));
        }
    }

    public string PeriodNote
    {
        get => _periodNote;
        set
        {
            if (!SetField(ref _periodNote, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(CanAddPeriodComponent));
        }
    }

    public async Task EnsureLoadedAsync()
    {
        if (_loaded || !HasReadAccess) return;
        await LoadAsync(null).ConfigureAwait(true);
    }

    public Task RefreshAsync() => LoadAsync(SelectedPeriod?.Id);

    public Task OpenPeriodAsync(PayrollPeriodRowView row) => LoadAsync(row.Source.Id);

    private async Task LoadAsync(string? payrollPeriodId)
    {
        if (!HasReadAccess || IsBusy) return;

        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            var data = await _service.GetAsync(payrollPeriodId).ConfigureAwait(true);
            Apply(data);
            _loaded = true;
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Không tải được dữ liệu nền tính lương."), true);
        }
        finally
        {
            IsBusy = false;
            RaiseAccess();
            RaiseSummary();
            RaiseEmptyStates();
        }
    }

    private void Apply(PayrollFoundationData data)
    {
        _capabilities = data.Capabilities ?? new PayrollCapabilitiesData();
        SelectedPeriod = data.SelectedPeriod;

        RebuildAttendanceSources(data.AttendanceSources ?? []);
        RebuildEmployees(data.Employees ?? [], data.PeriodEmployees ?? []);
        RebuildComponentOptions(data.ComponentTypes ?? []);
        RebuildPeriods(data.Periods ?? []);
        RebuildSalaryRows(data.SalaryProfiles ?? []);
        RebuildFixedRows(data.FixedComponents ?? []);
        RebuildComponentTypeRows(data.ComponentTypes ?? []);
        RebuildPeriodComponentRows(data.PeriodComponents ?? []);
        ApplyAggregation(data.Calculation);
    }

    private void RebuildAttendanceSources(IEnumerable<PayrollAttendanceSourceData> items)
    {
        var selectedId = SelectedAttendanceSource?.Source.Id;
        AttendanceSources.Clear();
        foreach (var item in items)
        {
            AttendanceSources.Add(new PayrollAttendanceSourceOption(
                item,
                $"{PayrollFoundationPresentation.PeriodText(item.PeriodStart, item.PeriodEnd, item.BranchName)} · Bản chốt lần {item.Revision}"));
        }
        SelectedAttendanceSource = AttendanceSources.FirstOrDefault(item => item.Source.Id == selectedId)
            ?? AttendanceSources.FirstOrDefault();
    }

    private void RebuildEmployees(IEnumerable<PayrollEmployeeData> employees, IEnumerable<PayrollEmployeeData> periodEmployees)
    {
        var salaryId = SalaryEmployee?.Source.Id;
        var fixedId = FixedEmployee?.Source.Id;
        var periodId = PeriodEmployee?.Source.Id;

        Employees.Clear();
        foreach (var item in employees)
            Employees.Add(new PayrollEmployeeOption(item, $"{item.Code} · {item.FullName}"));

        PeriodEmployees.Clear();
        foreach (var item in periodEmployees)
            PeriodEmployees.Add(new PayrollEmployeeOption(item, $"{item.Code} · {item.FullName}"));

        SalaryEmployee = Employees.FirstOrDefault(item => item.Source.Id == salaryId);
        FixedEmployee = Employees.FirstOrDefault(item => item.Source.Id == fixedId);
        PeriodEmployee = PeriodEmployees.FirstOrDefault(item => item.Source.Id == periodId);
    }

    private void RebuildComponentOptions(IEnumerable<PayrollComponentTypeData> items)
    {
        var fixedId = FixedComponent?.Source.Id;
        var periodId = PeriodComponent?.Source.Id;

        FixedComponentOptions.Clear();
        PeriodComponentOptions.Clear();
        foreach (var item in items.Where(item => item.IsActive && item.InputMode == "MANUAL"))
        {
            var option = new PayrollComponentOption(
                item,
                $"{item.Name} · {PayrollFoundationPresentation.CategoryLabel(item.Category)}");
            if (item.Recurrence == "FIXED") FixedComponentOptions.Add(option);
            if (item.Recurrence == "PERIOD") PeriodComponentOptions.Add(option);
        }

        FixedComponent = FixedComponentOptions.FirstOrDefault(item => item.Source.Id == fixedId);
        PeriodComponent = PeriodComponentOptions.FirstOrDefault(item => item.Source.Id == periodId);
    }

    private void RebuildPeriods(IEnumerable<PayrollPeriodData> items)
    {
        PeriodRows.Clear();
        foreach (var item in items)
        {
            PeriodRows.Add(new PayrollPeriodRowView(
                item,
                PayrollFoundationPresentation.PeriodText(item.PeriodStart, item.PeriodEnd, item.BranchName),
                $"Bản chốt công lần {item.AttendanceRevision}",
                PayrollFoundationPresentation.PeriodStatusLabel(item.Status),
                item.CurrencyCode));
        }
    }

    private void RebuildSalaryRows(IEnumerable<PayrollSalaryProfileData> items)
    {
        SalaryRows.Clear();
        foreach (var item in items)
        {
            SalaryRows.Add(new PayrollSalaryRowView(
                item,
                $"{item.EmployeeCode} · {item.EmployeeName}",
                PayrollFoundationPresentation.ScopeText(item.BranchName),
                PayrollFoundationPresentation.MoneyText(item.MonthlySalary),
                PayrollFoundationPresentation.EffectiveText(item.EffectiveFrom, item.EffectiveTo),
                item.Note ?? "—"));
        }
    }

    private void RebuildFixedRows(IEnumerable<PayrollFixedComponentData> items)
    {
        FixedRows.Clear();
        foreach (var item in items)
        {
            FixedRows.Add(new PayrollFixedComponentRowView(
                item,
                $"{item.EmployeeCode} · {item.EmployeeName}",
                item.ComponentName,
                PayrollFoundationPresentation.CategoryLabel(item.Category),
                PayrollFoundationPresentation.MoneyText(item.Amount),
                PayrollFoundationPresentation.EffectiveText(item.EffectiveFrom, item.EffectiveTo)));
        }
    }

    private void RebuildComponentTypeRows(IEnumerable<PayrollComponentTypeData> items)
    {
        ComponentTypeRows.Clear();
        foreach (var item in items)
        {
            var included = new List<string>();
            if (item.IncludeInGross) included.Add("Tổng thu nhập");
            if (item.IncludeInNet) included.Add("Thực nhận");

            ComponentTypeRows.Add(new PayrollComponentTypeRowView(
                item,
                $"{item.Name} · {item.Code}",
                PayrollFoundationPresentation.CategoryLabel(item.Category),
                $"{PayrollFoundationPresentation.RecurrenceLabel(item.Recurrence)}{(item.ProrateByWorkdays ? " · Theo ngày công" : string.Empty)}",
                PayrollFoundationPresentation.InputModeLabel(item.InputMode),
                included.Count > 0 ? string.Join(" · ", included) : "Không cộng vào tổng",
                PayrollFoundationPresentation.EffectiveText(item.EffectiveFrom, item.EffectiveTo)));
        }
    }

    private void RebuildPeriodComponentRows(IEnumerable<PayrollPeriodComponentData> items)
    {
        PeriodComponentRows.Clear();
        foreach (var item in items)
        {
            PeriodComponentRows.Add(new PayrollPeriodComponentRowView(
                item,
                $"{item.EmployeeCode} · {item.EmployeeName}",
                item.ComponentName,
                PayrollFoundationPresentation.CategoryLabel(item.Category),
                PayrollFoundationPresentation.MoneyText(item.Amount),
                item.Note));
        }
    }

    private static bool IsMoney(string value) => TryMoney(value, out _);

    private static bool TryMoney(string value, out string normalized)
    {
        normalized = (value ?? string.Empty).Trim().Replace(',', '.');
        if (!MoneyPattern.IsMatch(normalized)) return false;
        var parts = normalized.Split('.', 2);
        var whole = parts[0].TrimStart('0');
        if (whole.Length == 0) whole = "0";
        var fraction = parts.Length > 1 ? parts[1] : string.Empty;
        normalized = fraction.Length == 0 ? whole : $"{whole}.{fraction}";
        return true;
    }

    private string MutationKey(string slot)
    {
        if (_mutationKeys.TryGetValue(slot, out var existing)) return existing;
        var key = _idempotencyKeys.Create("payroll-foundation");
        _mutationKeys[slot] = key;
        return key;
    }

    private static string MutationSlot<T>(T payload) =>
        JsonSerializer.Serialize(payload);

    private static string CanonicalDate(DateTime value) =>
        value.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string? EmptyToNull(string? value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        return normalized.Length == 0 ? null : normalized;
    }

    private static string PublicError(Exception exception, string fallback)
    {
        if (exception is CanonicalApiException canonical)
            return CanonicalErrorMessages.WithRequestId(
                CanonicalErrorMessages.ToOfficeMessage(canonical),
                canonical.RequestId);
        return exception is InvalidOperationException && !string.IsNullOrWhiteSpace(exception.Message)
            ? exception.Message
            : fallback;
    }

    private void ResetForAccessChange()
    {
        _loaded = false;
        _mutationKeys.Clear();
        _capabilities = new PayrollCapabilitiesData();
        SelectedPeriod = null;
        AttendanceSources.Clear();
        PeriodRows.Clear();
        Employees.Clear();
        PeriodEmployees.Clear();
        FixedComponentOptions.Clear();
        PeriodComponentOptions.Clear();
        SalaryRows.Clear();
        FixedRows.Clear();
        ComponentTypeRows.Clear();
        PeriodComponentRows.Clear();
        SelectedAttendanceSource = null;
        SalaryEmployee = null;
        FixedEmployee = null;
        FixedComponent = null;
        PeriodEmployee = null;
        PeriodComponent = null;
        ResetAggregation();
        Message = string.Empty;
        MessageIsError = false;
        RaiseAccess();
        RaiseSummary();
        RaiseEmptyStates();
    }

    private void RaiseAccess()
    {
        OnPropertyChanged(nameof(HasReadAccess));
        OnPropertyChanged(nameof(CanManage));
        RaiseAvailability();
    }

    private void RaiseAvailability()
    {
        OnPropertyChanged(nameof(CanRefresh));
        OnPropertyChanged(nameof(CanCreatePeriod));
        OnPropertyChanged(nameof(CanSaveSalary));
        OnPropertyChanged(nameof(CanSaveFixed));
        OnPropertyChanged(nameof(CanCreateComponent));
        OnPropertyChanged(nameof(CanAddPeriodComponent));
        RaiseAggregationAvailability();
    }

    private void RaiseSummary()
    {
        OnPropertyChanged(nameof(PeriodCountText));
        OnPropertyChanged(nameof(AttendanceSourceCountText));
        OnPropertyChanged(nameof(EmployeeCountText));
        OnPropertyChanged(nameof(ComponentCountText));
    }

    private void RaiseEmptyStates()
    {
        OnPropertyChanged(nameof(ShowLoading));
        OnPropertyChanged(nameof(IsPeriodEmpty));
        OnPropertyChanged(nameof(IsSalaryEmpty));
        OnPropertyChanged(nameof(IsFixedEmpty));
        OnPropertyChanged(nameof(IsComponentTypeEmpty));
        OnPropertyChanged(nameof(IsPeriodComponentEmpty));
    }

    private void SetMessage(string message, bool isError)
    {
        MessageIsError = isError;
        Message = message;
    }

    private static void RunOnUiThread(Action action)
    {
        if (Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
            dispatcher.Invoke(action);
        else
            action();
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
