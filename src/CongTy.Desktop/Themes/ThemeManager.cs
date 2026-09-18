using System.Windows;
using System.Windows.Media;

namespace CongTy.Desktop.Themes;

public static class ThemeManager
{
    public static string Normalize(string? theme)
    {
        if (string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase)) return "Dark";
        if (string.Equals(theme, "Green", StringComparison.OrdinalIgnoreCase)) return "Green";
        return "Light";
    }

    public static string ToAppearanceKey(string? theme) =>
        Normalize(theme) switch
        {
            "Green" => "green",
            "Dark" => "dark",
            _ => "default"
        };

    public static string FromAppearanceKey(string? key) =>
        key?.Trim().ToLowerInvariant() switch
        {
            "green" => "Green",
            "dark" => "Dark",
            _ => "Light"
        };

    public static int NormalizeScale(int scale) => scale is >= -4 and <= 4 ? scale : 0;

    public static void Apply(string? theme)
    {
        var normalized = Normalize(theme);
        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var themeDictionary = dictionaries.FirstOrDefault(
            dictionary => dictionary.Source?.OriginalString.Contains("Theme.", StringComparison.OrdinalIgnoreCase) == true);

        var replacement = new ResourceDictionary
        {
            Source = new Uri($"Themes/Theme.{normalized}.xaml", UriKind.Relative)
        };

        if (themeDictionary is null)
        {
            dictionaries.Insert(0, replacement);
            return;
        }

        var index = dictionaries.IndexOf(themeDictionary);
        dictionaries[index] = replacement;
    }

    public static void ApplyScale(int scale)
    {
        var normalized = NormalizeScale(scale);
        var factor = 1d + (normalized * 0.04d);
        if (Application.Current?.MainWindow?.Content is not FrameworkElement content) return;

        content.LayoutTransform = Math.Abs(factor - 1d) < 0.0001d
            ? Transform.Identity
            : new ScaleTransform(factor, factor);
    }
}
