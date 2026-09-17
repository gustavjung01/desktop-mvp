using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using CongTy.Contracts;

namespace CongTy.Desktop.Sales;

public sealed record ManagementProposalOption(string Value, string Label);

public static class ManagementProposalPresentation
{
    public static string StatusLabel(string? value) => value switch
    {
        "pending" => "Chờ quyết định",
        "needs-info" => "Chờ bổ sung",
        "approved" => "Đã đồng ý",
        "rejected" => "Đã từ chối",
        _ => value?.Trim() ?? "—"
    };

    public static string DomainLabel(string? value) => value switch
    {
        "commercial" => "Thương mại",
        "customer-debt" => "Khách hàng & công nợ",
        "operations" => "Vận hành",
        _ => value?.Trim() ?? "—"
    };

    public static string FormatDateTime(string? value)
    {
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            return "—";
        try
        {
            TimeZoneInfo zone;
            try { zone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
            catch { zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
            var local = TimeZoneInfo.ConvertTime(parsed, zone);
            return local.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("vi-VN"));
        }
        catch
        {
            return parsed.ToLocalTime().ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("vi-VN"));
        }
    }

    public static ManagementProposalRow Row(ManagementProposalData data) => new(data);
}

public sealed class ManagementProposalRow : INotifyPropertyChanged
{
    private string _resubmitContent;
    private string _resubmitReason;
    private string _resubmitEvidence;
    private string _error = string.Empty;
    private bool _isResubmitting;

    public ManagementProposalRow(ManagementProposalData data)
    {
        Data = data;
        _resubmitContent = data.Content;
        _resubmitReason = data.Reason;
        _resubmitEvidence = string.Join(Environment.NewLine, data.Evidence);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ManagementProposalData Data { get; }
    public string Id => Data.Id;
    public string Status => Data.Status;
    public string StatusLabel => ManagementProposalPresentation.StatusLabel(Data.Status);
    public string DomainLabel => ManagementProposalPresentation.DomainLabel(Data.Domain);
    public string Meta => string.IsNullOrWhiteSpace(Data.EntityLabel) ? DomainLabel : $"{DomainLabel} · {Data.EntityLabel}";
    public string Title => Data.Title;
    public string Content => Data.Content;
    public string Impact => Data.Impact;
    public bool HasImpact => !string.IsNullOrWhiteSpace(Impact);
    public string UpdatedAt => ManagementProposalPresentation.FormatDateTime(Data.UpdatedAt);
    public string DecisionNote => Data.DecisionNote ?? string.Empty;
    public bool HasDecisionNote => !string.IsNullOrWhiteSpace(DecisionNote);
    public bool CanResubmit => string.Equals(Data.Status, "needs-info", StringComparison.Ordinal);

    public string ResubmitContent
    {
        get => _resubmitContent;
        set
        {
            if (SetField(ref _resubmitContent, value ?? string.Empty))
                OnPropertyChanged(nameof(CanSubmitResubmission));
        }
    }

    public string ResubmitReason { get => _resubmitReason; set => SetField(ref _resubmitReason, value ?? string.Empty); }
    public string ResubmitEvidence { get => _resubmitEvidence; set => SetField(ref _resubmitEvidence, value ?? string.Empty); }

    public string Error
    {
        get => _error;
        set
        {
            if (SetField(ref _error, value ?? string.Empty))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(Error);

    public bool IsResubmitting
    {
        get => _isResubmitting;
        set
        {
            if (!SetField(ref _isResubmitting, value)) return;
            OnPropertyChanged(nameof(ResubmitButtonText));
            OnPropertyChanged(nameof(CanSubmitResubmission));
        }
    }

    public string ResubmitButtonText => IsResubmitting ? "Đang gửi…" : "Gửi bổ sung";
    public bool CanSubmitResubmission => CanResubmit && !IsResubmitting && !string.IsNullOrWhiteSpace(ResubmitContent);

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
