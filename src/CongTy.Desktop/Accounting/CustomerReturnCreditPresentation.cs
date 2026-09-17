using System.Globalization;
using CongTy.Contracts;

namespace CongTy.Desktop.Accounting;

public static class CustomerReturnCreditPresentation
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public static decimal Amount(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) ? number : 0m;

    public static string Money(string? value, string? currency)
    {
        var amount = Amount(value);
        var code = string.IsNullOrWhiteSpace(currency) ? "VND" : currency.Trim().ToUpperInvariant();
        return code == "VND"
            ? $"{amount.ToString("N0", Vi)} ₫"
            : $"{amount.ToString("0.######", Vi)} {code}";
    }

    public static string Quantity(string? value) => Amount(value).ToString("0.######", Vi);

    public static string Date(string? value) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date)
            ? date.ToString("dd/MM/yyyy", Vi)
            : value ?? "—";

    public static string Party(string? code, string? name)
    {
        var safeCode = string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim();
        var safeName = string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();
        return (safeCode, safeName) switch
        {
            ("", "") => "—",
            ("", _) => safeName,
            (_, "") => safeCode,
            _ => $"{safeCode} — {safeName}"
        };
    }

    public static string Status(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "open" => "Còn giá trị chưa sử dụng",
        "partially_allocated" => "Đã sử dụng một phần",
        "settled" => "Đã sử dụng hết",
        "reversed" => "Đã đảo",
        _ => string.IsNullOrWhiteSpace(status) ? "—" : status
    };

    public static string RefundMethod(string? method) => method?.Trim().ToUpperInvariant() switch
    {
        "BANK_TRANSFER" => "Chuyển khoản",
        "CASH" => "Tiền mặt",
        _ => string.IsNullOrWhiteSpace(method) ? "—" : method
    };
}

public sealed record CustomerReturnCreditRow(
    int Sequence,
    string Id,
    string DocumentNumber,
    string SourceReturn,
    string Customer,
    string Warehouse,
    string PostingDate,
    string GrossAmount,
    string AllocatedAmount,
    string RefundedAmount,
    string RemainingAmount,
    string Status);

public sealed record CustomerReturnCreditLineRow(string Sku, string Product, string Quantity, string CreditAmount);
public sealed record CustomerReturnCreditAllocationRow(string TargetDocument, string Date, string Amount, string Status);
public sealed record CustomerRefundRow(string Id, string Number, string MethodDestination, string Date, string Amount, string Status, bool CanReverse);

public sealed class CustomerReturnCreditTargetRow
{
    private string _amountInput = string.Empty;
    public required string Id { get; init; }
    public required string CustomerId { get; init; }
    public required string CurrencyCode { get; init; }
    public required string Reference { get; init; }
    public required string Warehouse { get; init; }
    public required string RemainingAmount { get; init; }
    public string AmountInput { get => _amountInput; set => _amountInput = value ?? string.Empty; }
}
