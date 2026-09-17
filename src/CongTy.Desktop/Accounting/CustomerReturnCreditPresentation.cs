using System.ComponentModel;
using System.Globalization;

namespace CongTy.Desktop.Accounting;

public static class CustomerReturnCreditPresentation
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
        "open" => "Chưa sử dụng",
        "partially_allocated" => "Đã dùng một phần",
        "settled" => "Đã dùng hết",
        "reversed" => "Đã đảo",
        _ => "Chưa xác định"
    };

    public static string RefundMethod(string? method) => method switch
    {
        "BANK_TRANSFER" => "Chuyển khoản",
        "CASH" => "Tiền mặt",
        _ => "Phương thức khác"
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
        return DateOnly.TryParse(value, out var date) ? date.ToString("dd/MM/yyyy", Vi) : value.Trim();
    }
}

public sealed record CustomerReturnCreditRow(
    int Sequence,
    string Id,
    string ReturnNumber,
    string Customer,
    string Warehouse,
    string OriginalAmount,
    string RemainingAmount,
    string Status);

public sealed record CustomerReturnCreditLineRow(
    string Item,
    string SourceDocument,
    string Quantity,
    string AdjustmentAmount);

public sealed class CustomerReturnCreditTargetRow : INotifyPropertyChanged
{
    private string _amountInput = string.Empty;

    public CustomerReturnCreditTargetRow(
        string id,
        string documentNumber,
        string salesOrderNumber,
        string warehouse,
        string remainingAmount,
        string currencyCode)
    {
        Id = id;
        DocumentNumber = documentNumber;
        SalesOrderNumber = salesOrderNumber;
        Warehouse = warehouse;
        RemainingAmount = remainingAmount;
        CurrencyCode = currencyCode;
    }

    public string Id { get; }
    public string DocumentNumber { get; }
    public string SalesOrderNumber { get; }
    public string Warehouse { get; }
    public string RemainingAmount { get; }
    public string CurrencyCode { get; }
    public string Reference => string.IsNullOrWhiteSpace(SalesOrderNumber)
        ? DocumentNumber
        : $"{DocumentNumber} · {SalesOrderNumber}";

    public string AmountInput
    {
        get => _amountInput;
        set
        {
            if (_amountInput == value) return;
            _amountInput = value ?? string.Empty;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AmountInput)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public sealed record CustomerReturnCreditRefundRow(
    string Id,
    string RefundNumber,
    string MethodAndDestination,
    string ExternalReference,
    string Amount,
    string Status,
    bool CanReverse);
