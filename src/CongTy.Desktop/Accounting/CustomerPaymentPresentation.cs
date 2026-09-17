using System.Globalization;

namespace CongTy.Desktop.Accounting;

public static class CustomerPaymentPresentation
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public static decimal Amount(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) ? amount : 0m;

    public static string Money(string? value, string? currencyCode)
    {
        var currency = string.IsNullOrWhiteSpace(currencyCode) ? "VND" : currencyCode.Trim().ToUpperInvariant();
        var amount = Amount(value);
        if (currency == "VND") return $"{amount.ToString("N0", Vi)} {currency}";
        var formatted = amount.ToString("N6", Vi).TrimEnd('0').TrimEnd(Vi.NumberFormat.NumberDecimalSeparator.ToCharArray());
        return $"{formatted} {currency}";
    }

    public static string Status(string? status) => status switch
    {
        "open" => "Chưa gắn với đơn",
        "partially_allocated" => "Đã ghi một phần",
        "settled" => "Đã ghi nhận",
        "reversed" => "Đã hủy",
        _ => "Chưa xác định"
    };

    public static string PaymentMethod(string? method) => method switch
    {
        "CASH" => "Tiền mặt",
        "BANK_TRANSFER" => "Chuyển khoản",
        "OTHER" => "Khác",
        _ => "Khác"
    };

    public static string Party(string? code, string? name)
    {
        var left = string.IsNullOrWhiteSpace(code) ? "—" : code.Trim();
        var right = string.IsNullOrWhiteSpace(name) ? "—" : name.Trim();
        return $"{left} · {right}";
    }

    public static string Date(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "—";
        return DateOnly.TryParse(value, out var parsed) ? parsed.ToString("dd/MM/yyyy", Vi) : value.Trim();
    }
}

public sealed record CustomerPaymentChoice(string Id, string Display);
public sealed record CustomerPaymentRow(
    int Sequence,
    string Id,
    string DocumentNumber,
    string PaymentDate,
    string RemittingEmployee,
    string Customer,
    string Orders,
    string OriginalAmount,
    string RelatedRemainingAmount,
    string Status);

public sealed class CustomerPaymentTargetRow : System.ComponentModel.INotifyPropertyChanged
{
    private string _amountInput = string.Empty;

    public CustomerPaymentTargetRow(
        string id,
        string reference,
        string documentNumber,
        string sourceDate,
        string warehouse,
        string remainingAmount,
        string currencyCode)
    {
        Id = id;
        Reference = reference;
        DocumentNumber = documentNumber;
        SourceDate = sourceDate;
        Warehouse = warehouse;
        RemainingAmount = remainingAmount;
        CurrencyCode = currencyCode;
    }

    public string Id { get; }
    public string Reference { get; }
    public string DocumentNumber { get; }
    public string SourceDate { get; }
    public string Warehouse { get; }
    public string RemainingAmount { get; }
    public string CurrencyCode { get; }
    public string DocumentDate => $"{DocumentNumber} · {SourceDate}";
    public string AmountInput
    {
        get => _amountInput;
        set
        {
            if (_amountInput == value) return;
            _amountInput = value ?? string.Empty;
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(AmountInput)));
        }
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}

public sealed record CustomerPaymentAllocationRow(
    string Id,
    string TargetDocumentNumber,
    string Warehouse,
    string AllocationDate,
    string Amount,
    string Status,
    bool CanReverse);
