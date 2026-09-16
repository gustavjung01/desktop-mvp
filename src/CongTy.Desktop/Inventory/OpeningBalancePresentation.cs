using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using CongTy.ApiClient;
using CongTy.Contracts;

namespace CongTy.Desktop.Inventory;

public sealed record OpeningBalanceOption(string Id, string Label);

public sealed record OpeningBalanceCsvRowData(
    string Sku,
    string SourceQuantity,
    string LocationCode,
    string LotCode,
    string ManufacturedDate,
    string ExpiryDate,
    string SupplierLotReference,
    string SourceLineReference);

public sealed class OpeningBalancePreviewRow : INotifyPropertyChanged
{
    private readonly Action _draftChanged;
    private string _lotCode;
    private string _expiryDate;
    private string _defaultLocationCode = string.Empty;
    private string _warehouseLabel = "Chưa chọn";
    private OpeningBalanceValidationRowData? _resolved;
    private OpeningBalanceValidationErrorData? _serverError;
    private bool _validationCurrent;

    public OpeningBalancePreviewRow(int index, OpeningBalanceCsvRowData data, Action draftChanged)
    {
        Index = index;
        Data = data;
        _draftChanged = draftChanged;
        _lotCode = data.LotCode;
        _expiryDate = data.ExpiryDate;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public int Index { get; }
    public int ApiLine => Index + 1;
    public int FileLine => Index + 2;
    public OpeningBalanceCsvRowData Data { get; }

    public string Sku => string.IsNullOrWhiteSpace(_resolved?.SourceSku) ? Data.Sku : _resolved.SourceSku!;
    public string ProductName => string.IsNullOrWhiteSpace(_resolved?.ProductName) ? "—" : _resolved.ProductName!;
    public string Quantity => OpeningBalancePresentation.DisplayQuantity(
        string.IsNullOrWhiteSpace(_resolved?.SourceQuantity) ? Data.SourceQuantity : _resolved.SourceQuantity);
    public string Warehouse => !string.IsNullOrWhiteSpace(_resolved?.WarehouseCode)
        ? $"{_resolved.WarehouseCode} — {_resolved.WarehouseName ?? string.Empty}".TrimEnd()
        : _warehouseLabel;
    public string EffectiveLocationCode =>
        string.IsNullOrWhiteSpace(Data.LocationCode) ? _defaultLocationCode : Data.LocationCode;
    public string Location => !string.IsNullOrWhiteSpace(_resolved?.LocationCode)
        ? $"{_resolved.LocationCode}{(string.IsNullOrWhiteSpace(_resolved.LocationName) ? string.Empty : $" — {_resolved.LocationName}")}"
        : string.IsNullOrWhiteSpace(EffectiveLocationCode) ? "—" : EffectiveLocationCode;

    public string PolicyPrimary => OpeningBalancePresentation.LotPolicy(_resolved);
    public string PolicyHint => OpeningBalancePresentation.PolicyHint(_resolved);

    public string LotCode
    {
        get => _lotCode;
        set
        {
            var next = value?.TrimStart().ToUpperInvariant() ?? string.Empty;
            if (_lotCode == next) return;
            _lotCode = next;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LotDisplay));
            _draftChanged();
        }
    }

    public string LotDisplay => HasResolvedPolicy
        ? LotEditable ? LotCode : "Không áp dụng"
        : string.IsNullOrWhiteSpace(LotCode) ? "Đối chiếu SKU trước" : LotCode;

    public DateTime? ExpiryDateValue
    {
        get => DateTime.TryParseExact(
            _expiryDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : null;
        set
        {
            var next = value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
            if (_expiryDate == next) return;
            _expiryDate = next;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ExpiryDateText));
            OnPropertyChanged(nameof(ExpiryDisplay));
            _draftChanged();
        }
    }

    public string ExpiryDateText => _expiryDate;
    public string ExpiryDisplay => HasResolvedPolicy
        ? ExpiryEditable ? (string.IsNullOrWhiteSpace(_expiryDate) ? "Chưa nhập" : OpeningBalancePresentation.Date(_expiryDate))
            : "Không áp dụng"
        : string.IsNullOrWhiteSpace(_expiryDate) ? "Đối chiếu SKU trước" : OpeningBalancePresentation.Date(_expiryDate);

    public bool HasResolvedPolicy => !string.IsNullOrWhiteSpace(_resolved?.BaseVariantId)
        && !string.IsNullOrWhiteSpace(_resolved?.LotTrackingMode);
    public bool LotEditable => HasResolvedPolicy
        && string.Equals(_resolved?.LotTrackingMode, "REQUIRED", StringComparison.OrdinalIgnoreCase);
    public bool ExpiryEditable => HasResolvedPolicy
        && !string.Equals(_resolved?.ExpiryTrackingMode, "NONE", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(_resolved?.ExpiryTrackingMode);

    public string LocalError
    {
        get
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(Data.Sku)) missing.Add("SKU");
            if (string.IsNullOrWhiteSpace(Data.SourceQuantity)) missing.Add("số lượng");
            return missing.Count == 0 ? string.Empty : $"Thiếu {string.Join(", ", missing)}";
        }
    }

    public string Status
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(LocalError)) return LocalError;
            if (_serverError is not null) return OpeningBalancePresentation.RowError(_serverError);
            if (_validationCurrent) return "Đã đối chiếu";
            if (_resolved is not null) return "Cần kiểm tra lại sau khi sửa";
            return "Hợp lệ sơ bộ";
        }
    }

    public bool StatusIsError => !string.IsNullOrWhiteSpace(LocalError) || _serverError is not null;

    public void SetContext(string warehouseLabel, string defaultLocationCode)
    {
        _warehouseLabel = string.IsNullOrWhiteSpace(warehouseLabel) ? "Chưa chọn" : warehouseLabel;
        _defaultLocationCode = defaultLocationCode ?? string.Empty;
        OnPropertyChanged(nameof(Warehouse));
        OnPropertyChanged(nameof(EffectiveLocationCode));
        OnPropertyChanged(nameof(Location));
    }

    public void ApplyValidation(
        OpeningBalanceValidationRowData? resolved,
        OpeningBalanceValidationErrorData? error,
        bool current)
    {
        _resolved = resolved;
        _serverError = error;
        _validationCurrent = current;
        RaiseValidationState();
    }

    public void InvalidateValidation()
    {
        _serverError = null;
        _validationCurrent = false;
        RaiseValidationState();
    }

    public OpeningBalanceDraftRowData ToRequestRow() => new()
    {
        Sku = Data.Sku.Trim(),
        SourceQuantity = Data.SourceQuantity.Trim(),
        LocationCode = NullIfEmpty(EffectiveLocationCode),
        LotCode = NullIfEmpty(LotCode),
        ManufacturedDate = NullIfEmpty(Data.ManufacturedDate),
        ExpiryDate = NullIfEmpty(_expiryDate),
        SupplierLotReference = NullIfEmpty(Data.SupplierLotReference),
        SourceLineReference = NullIfEmpty(Data.SourceLineReference) ?? $"Dong-{FileLine}",
        Metadata = []
    };

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void RaiseValidationState()
    {
        OnPropertyChanged(nameof(Sku));
        OnPropertyChanged(nameof(ProductName));
        OnPropertyChanged(nameof(Quantity));
        OnPropertyChanged(nameof(Warehouse));
        OnPropertyChanged(nameof(Location));
        OnPropertyChanged(nameof(PolicyPrimary));
        OnPropertyChanged(nameof(PolicyHint));
        OnPropertyChanged(nameof(HasResolvedPolicy));
        OnPropertyChanged(nameof(LotEditable));
        OnPropertyChanged(nameof(ExpiryEditable));
        OnPropertyChanged(nameof(LotDisplay));
        OnPropertyChanged(nameof(ExpiryDisplay));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(StatusIsError));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed record OpeningBalanceHistoryRow(
    int Sequence,
    string SourceKey,
    string SourceFilename,
    int RowCount,
    string CreatedAt);

public static class OpeningBalancePresentation
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public static string LotPolicy(OpeningBalanceValidationRowData? row)
    {
        if (row is null || string.IsNullOrWhiteSpace(row.BaseVariantId)) return "Chưa đối chiếu";
        if (string.IsNullOrWhiteSpace(row.LotTrackingMode)) return "Chưa cấu hình chính sách";
        return string.Equals(row.LotTrackingMode, "REQUIRED", StringComparison.OrdinalIgnoreCase)
            ? "Quản lý theo lô"
            : "Không quản lý theo lô";
    }

    public static string PolicyHint(OpeningBalanceValidationRowData? row)
    {
        if (row is null || string.IsNullOrWhiteSpace(row.BaseVariantId)
            || string.IsNullOrWhiteSpace(row.ExpiryTrackingMode))
        {
            return string.Empty;
        }

        var expiry = row.ExpiryTrackingMode.ToUpperInvariant() switch
        {
            "REQUIRED" => "HSD bắt buộc",
            "OPTIONAL" => "HSD tùy chọn",
            _ => "Không quản lý HSD"
        };
        return row.LocationRequired == true ? $"{expiry} · Bắt buộc vị trí" : expiry;
    }

    public static string DisplayQuantity(string? value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0) return "—";
        if (!decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
        {
            return normalized;
        }
        return number.ToString("0.######", CultureInfo.InvariantCulture);
    }

    public static string Date(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "—";
        return DateTime.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date.ToString("dd/MM/yyyy", Vi)
            : value;
    }

    public static string DateTimeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "—";
        if (!DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out var date))
        {
            return value;
        }

        var local = TimeZoneInfo.ConvertTime(
            date,
            TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
        return local.ToString("dd/MM/yyyy HH:mm", Vi);
    }

    public static string RowError(OpeningBalanceValidationErrorData error) => error.Code switch
    {
        "SKU_REQUIRED" => "Thiếu SKU hợp lệ.",
        "SKU_NOT_FOUND" => "SKU không tồn tại hoặc đã ngừng sử dụng.",
        "SKU_AMBIGUOUS" => "SKU chưa duy nhất; cần kiểm tra lại danh mục hàng.",
        "LOCATION_NOT_ALLOWED" => "Kho này dùng tồn chung; để trống Vị trí.",
        "LOCATION_REQUIRED" => "Kho này quản lý vị trí; cần nhập Vị trí.",
        "INVALID_LOCATION_CODE" => "Mã vị trí không hợp lệ.",
        "LOCATION_NOT_FOUND" => "Vị trí không hoạt động hoặc không thuộc kho đã chọn.",
        "WAREHOUSE_SCOPE_DENIED" => "Kho nằm ngoài phạm vi được cấp quyền.",
        "WAREHOUSE_NOT_AVAILABLE" => "Kho không tồn tại hoặc đã ngừng sử dụng.",
        "WAREHOUSE_LOCATION_MODE_REQUIRED" => "Kho chưa thiết lập chế độ quản lý vị trí.",
        "SKU_UNIT_NOT_AVAILABLE" => "SKU, SKU tồn chuẩn hoặc đơn vị tính không còn khả dụng.",
        "CONVERSION_NOT_CONFIGURED" => "SKU chưa cấu hình quy đổi về đơn vị tồn kho.",
        "FRACTIONAL_QUANTITY_NOT_ALLOWED" => "Đơn vị này không cho phép số lượng lẻ.",
        "TRACKING_POLICY_NOT_FOUND" => "SKU chưa có chính sách lô và hạn sử dụng.",
        "BASE_VARIANT_NOT_AVAILABLE" => "SKU tồn chuẩn không còn khả dụng.",
        "LOT_NOT_ALLOWED" => "SKU này không quản lý lô; bỏ dữ liệu lô và hạn dùng.",
        "LOT_REQUIRED" => "SKU này bắt buộc có mã lô.",
        "EXPIRY_REQUIRED" => "SKU này bắt buộc có hạn sử dụng.",
        "EXPIRY_NOT_ALLOWED" => "SKU này không quản lý hạn sử dụng.",
        "LOT_NOT_FOUND" => "Không tìm thấy lô hàng.",
        "LOT_SKU_MISMATCH" => "Lô hàng không thuộc SKU đã chọn.",
        "LOT_EXPIRY_MISMATCH" => "Hạn sử dụng không khớp với lô đã có.",
        "INVALID_QUANTITY" => "Số lượng không hợp lệ hoặc không lớn hơn 0.",
        "DUPLICATE_IMPORT_SCOPE" => "Dòng bị trùng đúng cùng phạm vi tồn kho trong tệp.",
        _ => string.IsNullOrWhiteSpace(error.Message)
            ? "Dòng dữ liệu chưa hợp lệ."
            : error.Message
    };

    public static string ApiError(CanonicalApiException exception) => exception.Code switch
    {
        "OPENING_BALANCE_SOURCE_KEY_CONFLICT" => "Mã đợt dữ liệu đã được dùng cho nội dung khác. Hãy dùng mã đợt khác hoặc kiểm tra lại tệp.",
        "OPENING_BALANCE_VALIDATION_FAILED" => "Dữ liệu đã thay đổi hoặc chưa còn hợp lệ. Hãy kiểm tra tệp lại trước khi xác nhận.",
        "WAREHOUSE_SCOPE_DENIED" => "Kho đã chọn không còn trong phạm vi được cấp quyền.",
        "WAREHOUSE_LOCATION_MODE_REQUIRED" => "Kho chưa thiết lập chế độ quản lý vị trí.",
        "INVALID_ROWS" => "Tệp phải có từ 1 đến 1.000 dòng dữ liệu.",
        "INVALID_SOURCE_KEY" => "Mã đợt dữ liệu không hợp lệ.",
        "INVALID_CONTENT_CHECKSUM" => "Dữ liệu kiểm tra không còn hợp lệ. Hãy kiểm tra tệp lại.",
        _ => CanonicalErrorMessages.ToOfficeMessage(exception)
    };
}

public static class OpeningBalanceCsv
{
    private static readonly string[] Headers =
    [
        "sku",
        "sourceQuantity",
        "locationCode",
        "lotCode",
        "manufacturedDate",
        "expiryDate",
        "supplierLotReference",
        "sourceLineReference"
    ];

    private static readonly Dictionary<string, string> Aliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["SKU"] = "sku",
            ["sku"] = "sku",
            ["Số lượng"] = "sourceQuantity",
            ["sourceQuantity"] = "sourceQuantity",
            ["Vị trí"] = "locationCode",
            ["locationCode"] = "locationCode",
            ["Mã lô"] = "lotCode",
            ["lotCode"] = "lotCode",
            ["Ngày sản xuất"] = "manufacturedDate",
            ["manufacturedDate"] = "manufacturedDate",
            ["Hạn sử dụng"] = "expiryDate",
            ["expiryDate"] = "expiryDate",
            ["Mã lô nhà cung cấp"] = "supplierLotReference",
            ["supplierLotReference"] = "supplierLotReference",
            ["Tham chiếu dòng"] = "sourceLineReference",
            ["sourceLineReference"] = "sourceLineReference"
        };

    public const string TemplateText = "SKU,Số lượng,Vị trí";

    public static IReadOnlyList<OpeningBalanceCsvRowData> Parse(string text)
    {
        var lines = (text ?? string.Empty)
            .TrimStart('﻿')
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        if (lines.Length < 2) return [];

        var delimiter = DelimiterFor(lines[0]);
        var rawHeaders = ParseLine(lines[0], delimiter);
        var headers = rawHeaders
            .Select(header => Aliases.TryGetValue(header.Trim(), out var alias) ? alias : header.Trim())
            .ToArray();

        if (!headers.Contains("sku", StringComparer.Ordinal)
            || !headers.Contains("sourceQuantity", StringComparer.Ordinal))
        {
            return [];
        }

        var result = new List<OpeningBalanceCsvRowData>();
        foreach (var line in lines.Skip(1))
        {
            var cells = ParseLine(line, delimiter);
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 0; i < headers.Length; i++)
            {
                values[headers[i]] = i < cells.Count ? cells[i] : string.Empty;
            }

            string Get(string key) => values.TryGetValue(key, out var value) ? value : string.Empty;
            result.Add(new OpeningBalanceCsvRowData(
                Get("sku"),
                Get("sourceQuantity"),
                Get("locationCode"),
                Get("lotCode"),
                Get("manufacturedDate"),
                Get("expiryDate"),
                Get("supplierLotReference"),
                Get("sourceLineReference")));
        }

        return result;
    }

    private static char DelimiterFor(string line)
    {
        var best = ',';
        var bestCount = -1;
        foreach (var delimiter in new[] { ',', ';', '\t' })
        {
            var count = ParseLine(line, delimiter).Count - 1;
            if (count <= bestCount) continue;
            best = delimiter;
            bestCount = count;
        }

        return best;
    }

    private static List<string> ParseLine(string line, char delimiter)
    {
        var cells = new List<string>();
        var value = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"')
            {
                if (quoted && index + 1 < line.Length && line[index + 1] == '"')
                {
                    value.Append('"');
                    index++;
                }
                else
                {
                    quoted = !quoted;
                }
            }
            else if (character == delimiter && !quoted)
            {
                cells.Add(value.ToString().Trim());
                value.Clear();
            }
            else
            {
                value.Append(character);
            }
        }

        cells.Add(value.ToString().Trim());
        return cells;
    }
}

public static class OpeningBalanceChecksum
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Compute(OpeningBalanceOperatorDraftData draft)
    {
        var element = JsonSerializer.SerializeToElement(draft, JsonOptions);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(
                   stream,
                   new JsonWriterOptions
                   {
                       Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                       Indented = false
                   }))
        {
            WriteCanonical(writer, element);
        }

        return Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant();
    }

    private static void WriteCanonical(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(item => item.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray()) WriteCanonical(writer, item);
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }
}
