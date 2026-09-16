using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using CongTy.Contracts;

namespace CongTy.Desktop.Logistics;

public sealed record DeliveryAttemptTripRow(
    DeliveryTripData Data,
    string Number,
    string WarehouseDriver,
    string VehicleWorkload);

public sealed record DeliveryProofRow(
    DeliveryProofData Data,
    string Type,
    string CapturedAt,
    string Receiver,
    string Reference,
    string Note,
    string FileNotice,
    bool HasDownloadUrl);

public sealed class DeliveryAttemptRow : INotifyPropertyChanged
{
    private bool _isProofExpanded;
    private bool _isProofLoading;
    private IReadOnlyList<DeliveryProofRow> _proofs = [];

    public DeliveryAttemptRow(
        DeliveryAttemptData data,
        string point,
        string number,
        string customer,
        string result,
        string attemptedAt,
        string reason,
        string rescheduledFor,
        string note)
    {
        Data = data;
        Point = point;
        Number = number;
        Customer = customer;
        Result = result;
        AttemptedAt = attemptedAt;
        Reason = reason;
        RescheduledFor = rescheduledFor;
        Note = note;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public DeliveryAttemptData Data { get; }
    public string Point { get; }
    public string Number { get; }
    public string Customer { get; }
    public string Result { get; }
    public string AttemptedAt { get; }
    public string Reason { get; }
    public string RescheduledFor { get; }
    public string Note { get; }

    public bool HasReason => !string.IsNullOrWhiteSpace(Reason);
    public bool HasRescheduledFor => !string.IsNullOrWhiteSpace(RescheduledFor);
    public bool HasNote => !string.IsNullOrWhiteSpace(Note);

    public bool IsProofExpanded
    {
        get => _isProofExpanded;
        set
        {
            if (_isProofExpanded == value) return;
            _isProofExpanded = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ProofButtonText));
            OnPropertyChanged(nameof(ShowNoProof));
        }
    }

    public bool IsProofLoading
    {
        get => _isProofLoading;
        set
        {
            if (_isProofLoading == value) return;
            _isProofLoading = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ProofButtonText));
            OnPropertyChanged(nameof(CanToggleProof));
            OnPropertyChanged(nameof(ShowNoProof));
        }
    }

    public IReadOnlyList<DeliveryProofRow> Proofs
    {
        get => _proofs;
        set
        {
            _proofs = value ?? [];
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasProofs));
            OnPropertyChanged(nameof(ShowNoProof));
        }
    }

    public bool HasProofs => Proofs.Count > 0;
    public bool ShowNoProof => IsProofExpanded && !HasProofs && !IsProofLoading;
    public bool CanToggleProof => !IsProofLoading;
    public string ProofButtonText => IsProofLoading
        ? "Đang tải..."
        : IsProofExpanded ? "Ẩn bằng chứng" : "Xem bằng chứng tùy chọn";

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public static class DeliveryAttemptPresentation
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public static string Result(string? value) => value switch
    {
        "delivered_full" => "Giao đủ",
        "delivered_partial" => "Giao một phần",
        "failed" => "Không giao được",
        "rescheduled" => "Hẹn giao lại",
        _ => string.IsNullOrWhiteSpace(value) ? "Chưa ghi nhận" : value.Trim()
    };

    public static string ProofType(string? value) => value switch
    {
        "photo" => "Ảnh giao hàng",
        "signature" => "Tham chiếu chữ ký",
        "otp" => "Tham chiếu mã xác nhận",
        "manual_confirm" => "Xác nhận thủ công",
        _ => string.IsNullOrWhiteSpace(value) ? "Bằng chứng" : value.Trim()
    };

    public static string DateTimeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "Chưa ghi nhận";
        if (!DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
            return value.Trim();

        var local = TimeZoneInfo.ConvertTime(parsed, BusinessZone());
        return local.ToString("dd/MM/yyyy HH:mm", Vi);
    }

    public static DeliveryAttemptTripRow Trip(DeliveryTripData trip) => new(
        trip,
        string.IsNullOrWhiteSpace(trip.Number) ? "Chuyến giao" : trip.Number.Trim(),
        $"{trip.WarehouseCode ?? trip.WarehouseName ?? "Kho"} · {trip.DriverName ?? trip.DriverCode ?? "Tài xế"}",
        $"{trip.LicensePlate ?? trip.VehicleCode ?? "Chưa rõ xe"} · {trip.AssignmentCount ?? 0} phiếu");

    public static DeliveryAttemptRow Attempt(DeliveryAttemptData attempt) => new(
        attempt,
        $"Điểm {attempt.StopSequence}",
        string.IsNullOrWhiteSpace(attempt.DeliveryOrderNumber) ? "Thiếu mã phiếu giao" : attempt.DeliveryOrderNumber.Trim(),
        attempt.CustomerName ?? attempt.CustomerCode ?? "Khách hàng",
        Result(attempt.Result),
        DateTimeText(attempt.AttemptedAt),
        attempt.ReasonCode?.Trim() ?? string.Empty,
        DateTimeTextOrEmpty(attempt.RescheduledFor),
        attempt.Note?.Trim() ?? string.Empty);

    public static DeliveryProofRow Proof(DeliveryProofData proof)
    {
        var fileNotice = proof.File is null
            ? string.Empty
            : string.IsNullOrWhiteSpace(proof.File.DownloadUrl)
                ? "Ảnh đã lưu; liên kết tạm thời chưa khả dụng."
                : proof.File.FileName;

        return new DeliveryProofRow(
            proof,
            ProofType(proof.PodType),
            DateTimeText(proof.CapturedAt),
            proof.ReceiverName?.Trim() ?? string.Empty,
            proof.ConfirmationReference?.Trim() ?? string.Empty,
            proof.Note?.Trim() ?? string.Empty,
            fileNotice,
            !string.IsNullOrWhiteSpace(proof.File?.DownloadUrl));
    }

    private static string DateTimeTextOrEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : DateTimeText(value);

    private static TimeZoneInfo BusinessZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); }
        catch
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }
            catch { return TimeZoneInfo.Local; }
        }
    }
}
