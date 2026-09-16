using CongTy.Contracts;

namespace CongTy.Desktop.Inventory;

public sealed record TrackingPolicyModeOption(string Id, string Label);

public sealed record TrackingPolicyCandidateRow(
    int Sequence,
    InventoryTrackingPolicyCandidateData Candidate,
    InventoryTrackingPolicyData? Policy)
{
    public string Sku => Candidate.BaseSku;
    public string Product => $"{Candidate.ProductCode} · {Candidate.ProductName}";
    public string SetupStatus => Policy is null ? "Chưa thiết lập" : "Đã thiết lập";
    public string Lot => Policy is null ? "—" : InventoryTrackingPolicyPresentation.LotLabel(Policy.LotTrackingMode);
    public string Expiry => Policy is null ? "—" : InventoryTrackingPolicyPresentation.ExpiryLabel(Policy.ExpiryTrackingMode);
    public string ActionText => Policy is null ? "Thiết lập" : "Sửa";
}

public static class InventoryTrackingPolicyPresentation
{
    public static string LotLabel(string? value) =>
        string.Equals(value, "REQUIRED", StringComparison.OrdinalIgnoreCase)
            ? "Bắt buộc quản lý theo lô"
            : "Không quản lý theo lô";

    public static string ExpiryLabel(string? value) => value?.ToUpperInvariant() switch
    {
        "REQUIRED" => "Bắt buộc nhập hạn sử dụng",
        "OPTIONAL" => "Có thể nhập hạn sử dụng",
        _ => "Không quản lý hạn sử dụng"
    };

    public static string CandidateLabel(InventoryTrackingPolicyCandidateData candidate) =>
        $"{candidate.BaseSku} — {candidate.ProductName} · {(candidate.HasPolicy ? "đã thiết lập" : "chưa thiết lập")}";

    public static string SearchText(InventoryTrackingPolicyCandidateData candidate) =>
        string.Join(' ', new[]
        {
            candidate.BaseSku,
            candidate.BaseVariantName,
            candidate.ProductCode,
            candidate.ProductName,
            candidate.RelatedVariantSearchText
        }.Where(value => !string.IsNullOrWhiteSpace(value))).ToLowerInvariant();
}
