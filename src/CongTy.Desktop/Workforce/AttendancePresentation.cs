using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CongTy.Contracts;
using QRCoder;

namespace CongTy.Desktop.Workforce;

public static class AttendancePresentation
{
    public static string StatusLabel(string? value) =>
        (value ?? string.Empty).ToUpperInvariant() switch
        {
            "NOT_STARTED" => "Chưa vào làm",
            "WORKING" => "Đang làm việc",
            "OUTSIDE" => "Đang ra ngoài",
            "COMPLETE" => "Đã hoàn tất",
            _ => "Chưa sẵn sàng",
        };

    public static string NextActionLabel(string? value) =>
        (value ?? string.Empty).ToUpperInvariant() switch
        {
            "CHECK_IN" => "Ghi nhận vào làm",
            "EXIT" => "Chọn lý do rời nơi làm việc",
            "RETURN" => "Ghi nhận quay lại",
            _ => "Đã hoàn tất chấm công",
        };

    public static string AttendanceMethodLabel(string? value) =>
        (value ?? string.Empty).ToUpperInvariant() switch
        {
            "QR" => "Quét mã QR tại nơi làm việc",
            "FACE" => "Quét khuôn mặt tại máy chấm công",
            "QR_FACE" => "Quét mã QR hoặc quét khuôn mặt",
            "FACE_MANUAL" => "Quét khuôn mặt hoặc chấm công trực tiếp",
            "ALL" => "Mã QR, quét khuôn mặt hoặc chấm công trực tiếp",
            "MANUAL" => "Chấm công trực tiếp",
            "BOTH" => "Quét mã QR hoặc chấm công trực tiếp",
            "NONE" => "Không yêu cầu chấm công",
            _ => "—",
        };

    public static string EventLabel(AttendanceEventData value) =>
        value.EventType.ToUpperInvariant() switch
        {
            "CHECK_IN" => "Vào làm",
            "CHECK_OUT" => "Kết thúc làm việc",
            "RETURN" => "Quay lại nơi làm việc",
            "TEMP_EXIT" => MovementReasonLabel(value.MovementReason),
            _ => value.EventType,
        };

    public static string MovementReasonLabel(string? value) =>
        (value ?? string.Empty).ToUpperInvariant() switch
        {
            "END_WORK" => "Kết thúc ngày làm việc",
            "WORK_BUSINESS" => "Ra ngoài làm việc",
            "PERSONAL" => "Ra ngoài vì việc cá nhân",
            "BREAK" => "Nghỉ giữa ca",
            "OTHER" => "Lý do khác",
            _ => "Ra tạm thời",
        };

    public static string SourceLabel(string? value) =>
        (value ?? string.Empty).ToUpperInvariant() switch
        {
            "QR" => "Mã QR",
            "FACE" => "Máy chấm công khuôn mặt",
            "MANUAL" => "Chấm công trực tiếp",
            "ADJUSTMENT" => "Điều chỉnh",
            "SYSTEM" => "Hệ thống",
            _ => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim(),
        };

    public static AttendanceEventRowView EventRow(AttendanceEventData value, string? timeZone) =>
        new(
            EventLabel(value),
            WorkSchedulePresentation.DateTimeText(value.OccurredAt, timeZone),
            string.IsNullOrWhiteSpace(value.PointName) ? SourceLabel(value.Source) : value.PointName!,
            string.IsNullOrWhiteSpace(value.Note) ? "—" : value.Note!.Trim());

    public static ImageSource? RenderQr(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload)) return null;
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload.Trim(), QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(data);
        var bytes = qrCode.GetGraphic(8);
        using var stream = new MemoryStream(bytes, writable: false);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }
}

public sealed record AttendanceEventRowView(
    string EventText,
    string TimeText,
    string SourceText,
    string NoteText);

public sealed record AttendanceOption(string Key, string Label)
{
    public override string ToString() => Label;
}

public sealed record AttendanceBranchOption(string Id, string Label)
{
    public override string ToString() => Label;
}
