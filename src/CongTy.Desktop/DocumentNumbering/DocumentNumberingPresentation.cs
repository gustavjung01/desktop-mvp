using CongTy.Contracts;

namespace CongTy.Desktop.DocumentNumbering;

public sealed record NumberingOption(string Value, string Label);

public sealed class NumberSeriesRow(DocumentNumberSeriesData data, bool isSelected = false)
{
    public DocumentNumberSeriesData Data { get; } = data;
    public bool IsSelected { get; } = isSelected;
    public string Id => Data.Id;
    public string Rule => Data.Name;
    public string DocumentType => DocumentNumberingPresentation.DocumentTypeLabel(Data.DocumentType);
    public string RuleAndType => $"{Data.Name}\n{DocumentType}";
    public string NumberTemplate => Data.NumberTemplate;
    public string TemplateState => Data.FormatLocked ? $"{Data.NumberTemplate} · Đã cố định" : Data.NumberTemplate;
    public string ResetPolicy => DocumentNumberingPresentation.ResetPolicyLabel(Data.ResetPolicy);
    public int AllocationCount => Data.AllocationCount;
    public string Status => Data.IsActive ? "Đang sử dụng" : "Đã ngừng";
    public string ToggleAction => Data.IsActive ? "Ngừng sử dụng" : "Đưa vào sử dụng";
}

public sealed record NumberCounterRow(string PeriodKey, string NextCounter);

public sealed class NumberAllocationRow(DocumentNumberAllocationData data)
{
    public DocumentNumberAllocationData Data { get; } = data;
    public string DocumentNumber => Data.DocumentNumber;
    public string DocumentDate => DocumentNumberingPresentation.DateLabel(Data.DocumentDate);
    public string PeriodKey => Data.PeriodKey;
    public string CounterValue => Data.CounterValue;
    public string AllocatedAt => DocumentNumberingPresentation.DateTimeLabel(Data.AllocatedAt);
}

public static class DocumentNumberingPresentation
{
    public static readonly NumberingOption[] DocumentTypes =
    [
        new("SALES_ORDER", "Đơn bán hàng"),
        new("PURCHASE_ORDER", "Đơn mua hàng"),
        new("GOODS_RECEIPT", "Phiếu nhập kho"),
        new("GOODS_ISSUE", "Phiếu xuất kho"),
        new("DELIVERY_ORDER", "Phiếu giao hàng"),
        new("INVENTORY_TRANSFER", "Phiếu chuyển kho"),
        new("INVENTORY_ADJUSTMENT", "Phiếu điều chỉnh tồn kho"),
        new("CUSTOMER_RETURN", "Phiếu nhận hàng trả lại"),
        new("SUPPLIER_RETURN", "Phiếu trả hàng nhà cung cấp"),
        new("CUSTOMER_PAYMENT", "Phiếu thu"),
        new("SUPPLIER_PAYMENT", "Phiếu chi"),
        new("CUSTOMER_REFUND", "Phiếu hoàn tiền khách hàng"),
        new("INVOICE", "Hóa đơn")
    ];

    public static readonly NumberingOption[] ResetPolicies =
    [
        new("NONE", "Không đánh lại số"),
        new("YEARLY", "Theo năm"),
        new("MONTHLY", "Theo tháng")
    ];

    public static readonly NumberingOption[] StatusFilters =
    [
        new("all", "Tất cả trạng thái"),
        new("active", "Đang sử dụng"),
        new("inactive", "Đã ngừng sử dụng")
    ];

    public static string DocumentTypeLabel(string value) =>
        DocumentTypes.FirstOrDefault(item => item.Value == value)?.Label ?? "Loại chứng từ khác";

    public static string ResetPolicyLabel(string value) => value switch
    {
        "NONE" => "Không đánh lại số",
        "YEARLY" => "Đánh lại theo năm",
        "MONTHLY" => "Đánh lại theo tháng",
        _ => value
    };

    public static string DateLabel(string value) =>
        DateTime.TryParse(value, out var parsed) ? parsed.ToString("dd/MM/yyyy") : value;

    public static string DateTimeLabel(string value) =>
        DateTimeOffset.TryParse(value, out var parsed) ? parsed.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : value;
}
