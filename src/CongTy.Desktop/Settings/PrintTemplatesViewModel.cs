using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Settings;

public sealed class PrintFieldOption : INotifyPropertyChanged
{
    private bool _isSelected;

    public PrintFieldOption(DocumentPrintTemplateFieldData field, bool isSelected)
    {
        Key = field.Key;
        Label = field.Label;
        Required = field.Required;
        _isSelected = field.Required || isSelected;
    }

    public string Key { get; }
    public string Label { get; }
    public bool Required { get; }
    public bool CanToggle => !Required;
    public string DisplayLabel => Required ? $"{Label} · Luôn in" : Label;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            var normalized = Required || value;
            if (_isSelected == normalized) return;
            _isSelected = normalized;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed record PrintAlignmentOption(string Value, string Label)
{
    public override string ToString() => Label;
}

public sealed class PrintTemplatesViewModel : INotifyPropertyChanged
{
    private static readonly HashSet<string> OwnerRoles =
        new(["system:security-owner", "system:implementation-owner"], StringComparer.Ordinal);

    private readonly IDocumentPrintTemplateService _service;
    private readonly IAccessStateService _access;
    private readonly ICanonicalIdempotencyKeyProvider _idempotency;

    private DocumentPrintTemplateData? _selectedTemplate;
    private string _pageSize = "A4";
    private string _heading = string.Empty;
    private bool _headingVisible = true;
    private PrintAlignmentOption _headingAlign;
    private PrintAlignmentOption _titleAlign;
    private bool _loaded;
    private bool _isBusy;
    private bool _messageIsError;
    private string _message = string.Empty;
    private string? _pendingFingerprint;
    private string? _pendingKey;

    public PrintTemplatesViewModel(
        IDocumentPrintTemplateService service,
        IAccessStateService access,
        ICanonicalIdempotencyKeyProvider idempotency)
    {
        _service = service;
        _access = access;
        _idempotency = idempotency;
        _headingAlign = AlignmentOptions[0];
        _titleAlign = AlignmentOptions[2];

        _access.Changed += (_, _) => RunOnUiThread(() =>
        {
            _loaded = false;
            Templates.Clear();
            Fields.Clear();
            SelectedTemplate = null;
            Message = string.Empty;
            RaiseAccess();
            if (_access.Current.IsAuthenticated) _ = EnsureLoadedAsync();
        });
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DocumentPrintTemplateData> Templates { get; } = [];
    public ObservableCollection<PrintFieldOption> Fields { get; } = [];

    public IReadOnlyList<string> PageSizes { get; } = ["A4", "A5"];
    public IReadOnlyList<PrintAlignmentOption> AlignmentOptions { get; } =
    [
        new("left", "Trái"),
        new("center", "Giữa"),
        new("right", "Phải")
    ];

    public bool IsAuthenticated => _access.Current.IsAuthenticated;
    public bool IsOwner => _access.Current.Roles.Any(OwnerRoles.Contains);
    public bool CanManage => IsOwner || _access.HasPermission("core.print-template.manage");
    public bool IsBusy { get => _isBusy; private set => Set(ref _isBusy, value, nameof(IsBusy), nameof(CanSave), nameof(CanReset)); }
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool MessageIsError { get => _messageIsError; private set => Set(ref _messageIsError, value); }
    public string Message { get => _message; private set => Set(ref _message, value, nameof(Message), nameof(HasMessage)); }

    public DocumentPrintTemplateData? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            if (ReferenceEquals(_selectedTemplate, value)) return;
            _selectedTemplate = value;
            OnPropertyChanged();
            ApplySelection(value);
        }
    }

    public string PageSize { get => _pageSize; set => Set(ref _pageSize, value); }
    public string Heading { get => _heading; set => Set(ref _heading, value, nameof(Heading), nameof(PreviewHeading)); }
    public bool HeadingVisible { get => _headingVisible; set => Set(ref _headingVisible, value, nameof(HeadingVisible), nameof(PreviewHeadingVisible)); }
    public PrintAlignmentOption HeadingAlign { get => _headingAlign; set => Set(ref _headingAlign, value, nameof(HeadingAlign), nameof(HeadingTextAlignment)); }
    public PrintAlignmentOption TitleAlign { get => _titleAlign; set => Set(ref _titleAlign, value, nameof(TitleAlign), nameof(TitleTextAlignment)); }
    public System.Windows.TextAlignment HeadingTextAlignment => ToTextAlignment(HeadingAlign.Value);
    public System.Windows.TextAlignment TitleTextAlignment => ToTextAlignment(TitleAlign.Value);

    public string PreviewHeading => string.IsNullOrWhiteSpace(Heading) ? FallbackHeading(SelectedTemplate) : Heading.Trim();
    public bool PreviewHeadingVisible => HeadingVisible && !string.IsNullOrWhiteSpace(PreviewHeading);
    public string PreviewTitle => SelectedTemplate?.Title?.Trim() is { Length: > 0 } title ? title : SelectedTemplate?.Name ?? string.Empty;
    public string TemplateStatus => SelectedTemplate is null
        ? string.Empty
        : SelectedTemplate.IsCustomized ? "Đang dùng cấu hình riêng" : "Đang dùng mặc định";
    public string PreviewFields => Fields.Count == 0
        ? "Chưa có thông tin để xem trước."
        : string.Join("  •  ", Fields.Where(item => item.IsSelected).Take(8).Select(item => item.Label));
    public bool CanSave => CanManage && SelectedTemplate is not null && !IsBusy && Fields.Any(item => item.IsSelected);
    public bool CanReset => CanManage && SelectedTemplate?.IsCustomized == true && !IsBusy;

    public async Task EnsureLoadedAsync()
    {
        if (_loaded || !IsAuthenticated) return;
        await RefreshAsync();
    }

    public async Task RefreshAsync(string? preferredKey = null)
    {
        if (!IsAuthenticated || IsBusy) return;
        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            var items = await _service.ListAsync();
            RunOnUiThread(() =>
            {
                Templates.Clear();
                foreach (var item in items) Templates.Add(item);
                var currentKey = preferredKey ?? TemplateKey(SelectedTemplate);
                SelectedTemplate = Templates.FirstOrDefault(item => TemplateKey(item) == currentKey)
                    ?? Templates.FirstOrDefault();
                _loaded = true;
            });
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Chưa thể tải cấu hình mẫu in."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task SaveAsync()
    {
        if (!CanSave || SelectedTemplate is null) return;
        var selected = SelectedTemplate;
        var visible = Fields.Where(item => item.IsSelected).Select(item => item.Key).ToArray();
        var request = new DocumentPrintTemplateUpdateRequest
        {
            PageSize = PageSize,
            VisibleFieldKeys = visible,
            Heading = string.IsNullOrWhiteSpace(Heading) ? null : Heading.Trim(),
            HeadingVisible = HeadingVisible,
            HeadingAlign = HeadingAlign.Value,
            TitleAlign = TitleAlign.Value,
            ExpectedUpdatedAt = selected.UpdatedAt
        };
        var fingerprint = BuildFingerprint("save", selected, request.PageSize, request.VisibleFieldKeys, request.Heading,
            request.HeadingVisible, request.HeadingAlign, request.TitleAlign, request.ExpectedUpdatedAt);
        var key = ReuseKey(fingerprint, "print-template-save");

        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            var saved = await _service.SaveAsync(selected.DocumentType, selected.TemplateCode, request, key);
            ClearPendingKey();
            ReplaceTemplate(saved);
            SetMessage("Đã lưu cấu hình mẫu in dùng chung.", false);
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Chưa thể lưu cấu hình mẫu in."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ResetAsync()
    {
        if (!CanReset || SelectedTemplate is null) return;
        var selected = SelectedTemplate;
        var request = new DocumentPrintTemplateResetRequest
        {
            ResetToDefault = true,
            ExpectedUpdatedAt = selected.UpdatedAt
        };
        var fingerprint = BuildFingerprint("reset", selected, request.ExpectedUpdatedAt);
        var key = ReuseKey(fingerprint, "print-template-reset");

        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            var saved = await _service.ResetAsync(selected.DocumentType, selected.TemplateCode, request, key);
            ClearPendingKey();
            ReplaceTemplate(saved);
            SetMessage("Đã khôi phục mẫu in mặc định.", false);
        }
        catch (Exception exception)
        {
            SetMessage(PublicError(exception, "Chưa thể khôi phục mẫu in mặc định."), true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ReplaceTemplate(DocumentPrintTemplateData saved)
    {
        RunOnUiThread(() =>
        {
            var key = TemplateKey(saved);
            var index = -1;
            for (var itemIndex = 0; itemIndex < Templates.Count; itemIndex++)
            {
                if (!string.Equals(TemplateKey(Templates[itemIndex]), key, StringComparison.Ordinal)) continue;
                index = itemIndex;
                break;
            }

            if (index >= 0) Templates[index] = saved;
            else Templates.Add(saved);
            SelectedTemplate = saved;
        });
    }

    private void ApplySelection(DocumentPrintTemplateData? template)
    {
        Fields.Clear();
        if (template is null)
        {
            PageSize = "A4";
            Heading = string.Empty;
            HeadingVisible = true;
            HeadingAlign = AlignmentOptions[0];
            TitleAlign = AlignmentOptions[2];
        }
        else
        {
            PageSize = string.Equals(template.PageSize, "A5", StringComparison.OrdinalIgnoreCase) ? "A5" : "A4";
            Heading = template.Heading ?? FallbackHeading(template);
            HeadingVisible = template.HeadingVisible;
            HeadingAlign = AlignmentOptions.FirstOrDefault(item => item.Value == template.HeadingAlign) ?? AlignmentOptions[0];
            TitleAlign = AlignmentOptions.FirstOrDefault(item => item.Value == template.TitleAlign) ?? AlignmentOptions[2];
            var selected = template.VisibleFieldKeys.ToHashSet(StringComparer.Ordinal);
            foreach (var field in template.Fields)
            {
                var option = new PrintFieldOption(field, selected.Contains(field.Key));
                option.PropertyChanged += (_, _) =>
                {
                    OnPropertyChanged(nameof(PreviewFields));
                    OnPropertyChanged(nameof(CanSave));
                };
                Fields.Add(option);
            }
        }

        OnPropertyChanged(nameof(TemplateStatus));
        OnPropertyChanged(nameof(PreviewTitle));
        OnPropertyChanged(nameof(PreviewHeading));
        OnPropertyChanged(nameof(PreviewHeadingVisible));
        OnPropertyChanged(nameof(PreviewFields));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanReset));
    }

    private void RaiseAccess()
    {
        OnPropertyChanged(nameof(IsAuthenticated));
        OnPropertyChanged(nameof(IsOwner));
        OnPropertyChanged(nameof(CanManage));
        OnPropertyChanged(nameof(CanSave));
        OnPropertyChanged(nameof(CanReset));
    }

    private string ReuseKey(string fingerprint, string scope)
    {
        if (string.Equals(_pendingFingerprint, fingerprint, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(_pendingKey))
            return _pendingKey;

        _pendingFingerprint = fingerprint;
        _pendingKey = _idempotency.Create(scope);
        return _pendingKey;
    }

    private void ClearPendingKey()
    {
        _pendingFingerprint = null;
        _pendingKey = null;
    }

    private static string BuildFingerprint(string action, DocumentPrintTemplateData template, params object?[] parts)
    {
        var builder = new StringBuilder(action)
            .Append('|').Append(template.DocumentType)
            .Append('|').Append(template.TemplateCode);
        foreach (var part in parts)
        {
            if (part is IEnumerable<string> values) builder.Append('|').Append(string.Join(",", values));
            else builder.Append('|').Append(part?.ToString() ?? "<null>");
        }
        return builder.ToString();
    }

    private static System.Windows.TextAlignment ToTextAlignment(string value) =>
        value switch
        {
            "center" => System.Windows.TextAlignment.Center,
            "right" => System.Windows.TextAlignment.Right,
            _ => System.Windows.TextAlignment.Left
        };

    private static string TemplateKey(DocumentPrintTemplateData? template) =>
        template is null ? string.Empty : $"{template.DocumentType}:{template.TemplateCode}";

    private static string FallbackHeading(DocumentPrintTemplateData? template) =>
        string.Equals(template?.DocumentType, "SALES_ORDER", StringComparison.Ordinal) ? "Hưng Phát" : string.Empty;

    private static string PublicError(Exception exception, string fallback) =>
        exception is CanonicalApiException ? exception.Message : fallback;

    private void SetMessage(string message, bool isError)
    {
        MessageIsError = isError;
        Message = message;
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess()) action();
        else dispatcher.Invoke(action);
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null, params string[] dependentProperties)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        foreach (var dependent in dependentProperties) OnPropertyChanged(dependent);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
