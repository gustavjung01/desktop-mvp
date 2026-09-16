using System.Diagnostics;

namespace CongTy.Desktop.Pricing;

public static class PricingOperationsNavigator
{
    public const string DataExchangePath = "/operations/data-exchange?tab=pricing";
    public const string ImportHistoryPath = "/operations/import-export-history?definitionKey=pricing-items";
    private const string DefaultCompanyWebBaseUrl = "https://npp-platform.vercel.app";

    public static bool TryOpenDataExchange(out string errorMessage) => TryOpen(DataExchangePath, out errorMessage);
    public static bool TryOpenImportHistory(out string errorMessage) => TryOpen(ImportHistoryPath, out errorMessage);

    private static bool TryOpen(string path, out string errorMessage)
    {
        errorMessage = string.Empty;
        try
        {
            var configured = Environment.GetEnvironmentVariable("CONGTY_WEB_BASE_URL");
            var baseUrl = string.IsNullOrWhiteSpace(configured) ? DefaultCompanyWebBaseUrl : configured.Trim();
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri)
                || baseUri.Scheme != Uri.UriSchemeHttps
                || !string.IsNullOrEmpty(baseUri.UserInfo))
            {
                errorMessage = "Địa chỉ Công Ty Web chưa hợp lệ.";
                return false;
            }

            var target = new Uri(new Uri(baseUri.GetLeftPart(UriPartial.Authority).TrimEnd('/') + "/"), path.TrimStart('/'));
            Process.Start(new ProcessStartInfo(target.AbsoluteUri) { UseShellExecute = true });
            return true;
        }
        catch
        {
            errorMessage = "Không mở được Công Ty Web. Vui lòng kiểm tra trình duyệt mặc định.";
            return false;
        }
    }
}
