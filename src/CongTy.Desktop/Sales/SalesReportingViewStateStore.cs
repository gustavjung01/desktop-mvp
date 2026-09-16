using System.IO;
using System.Text.Json;

namespace CongTy.Desktop.Sales;

public sealed record SalesReportingSavedView(
    string Dimension,
    string Search,
    string Currency,
    string Comparison);

public interface ISalesReportingViewStateStore
{
    SalesReportingSavedView? Load();
    Task SaveAsync(SalesReportingSavedView view, CancellationToken cancellationToken = default);
}

public sealed class SalesReportingViewStateStore : ISalesReportingViewStateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CongTy",
        "Desktop",
        "sales-reporting-view.json");

    public SalesReportingSavedView? Load()
    {
        try
        {
            if (!File.Exists(_path)) return null;
            var saved = JsonSerializer.Deserialize<SalesReportingSavedView>(File.ReadAllText(_path), JsonOptions);
            if (saved is null || !IsDimension(saved.Dimension) || !IsComparison(saved.Comparison)) return null;
            return saved with
            {
                Search = Limit(saved.Search, 80),
                Currency = Limit(saved.Currency, 16)
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveAsync(SalesReportingSavedView view, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(view);
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var json = JsonSerializer.Serialize(view, JsonOptions);
        await File.WriteAllTextAsync(_path, json, cancellationToken).ConfigureAwait(false);
    }

    private static string Limit(string? value, int maxLength)
    {
        var normalized = (value ?? string.Empty).Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static bool IsDimension(string? value) =>
        SalesReportingPresentation.Dimensions.Any(option => string.Equals(option.Key, value, StringComparison.Ordinal));

    private static bool IsComparison(string? value) =>
        SalesReportingPresentation.Comparisons.Any(option => string.Equals(option.Key, value, StringComparison.Ordinal));
}
