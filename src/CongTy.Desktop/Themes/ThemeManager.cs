using System.Windows;

namespace CongTy.Desktop.Themes;

public static class ThemeManager
{
    public static string Normalize(string? theme) =>
        string.Equals(theme, "Dark", StringComparison.OrdinalIgnoreCase) ? "Dark" : "Light";

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
}
