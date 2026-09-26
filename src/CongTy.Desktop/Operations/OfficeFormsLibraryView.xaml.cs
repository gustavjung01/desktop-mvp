using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;

namespace CongTy.Desktop.Operations;

public partial class OfficeFormsLibraryView : UserControl, INotifyPropertyChanged
{
    private readonly IReadOnlyList<OfficeFormDefinition> _catalog = OfficeFormsCatalog.All;
    private string _searchText = string.Empty;
    private OfficeFormGroupFilter? _selectedGroup;
    private string _notice = string.Empty;

    public OfficeFormsLibraryView()
    {
        InitializeComponent();
        GroupFilters = OfficeFormsCatalog.Groups;
        _selectedGroup = GroupFilters[0];
        FormsView = CollectionViewSource.GetDefaultView(_catalog);
        FormsView.Filter = Matches;
        DataContext = this;
        RefreshView();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<OfficeFormGroupFilter> GroupFilters { get; }
    public ICollectionView FormsView { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetField(ref _searchText, value ?? string.Empty)) return;
            RefreshView();
        }
    }

    public OfficeFormGroupFilter? SelectedGroup
    {
        get => _selectedGroup;
        set
        {
            if (!SetField(ref _selectedGroup, value)) return;
            RefreshView();
        }
    }

    public string CountText => $"{_catalog.Count(item => Matches(item))} / {_catalog.Count} biểu mẫu";

    public string Notice
    {
        get => _notice;
        private set => SetField(ref _notice, value ?? string.Empty);
    }

    private bool Matches(object item) =>
        item is OfficeFormDefinition form && Matches(form);

    private bool Matches(OfficeFormDefinition form)
    {
        if (SelectedGroup is { Key: not "all" } group
            && !string.Equals(form.GroupKey, group.Key, StringComparison.Ordinal))
            return false;

        var term = Normalize(SearchText);
        if (term.Length == 0) return true;
        return Normalize($"{form.Name} {form.Purpose} {form.GroupName}").Contains(term, StringComparison.Ordinal);
    }

    private void RefreshView()
    {
        FormsView.Refresh();
        OnPropertyChanged(nameof(CountText));
    }

    private void DownloadXlsx_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: OfficeFormDefinition { Xlsx: not null } form }) return;

        var dialog = new SaveFileDialog
        {
            FileName = SafeFileName(form.Name) + ".xlsx",
            AddExtension = true,
            DefaultExt = ".xlsx",
            Filter = "Excel (*.xlsx)|*.xlsx"
        };
        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;

        try
        {
            var spec = form.Xlsx!;
            var rows = Enumerable.Range(0, spec.BlankRows)
                .Select(_ => Enumerable.Repeat(string.Empty, spec.Headers.Length).ToArray())
                .ToArray();
            DataExchangeFileHelper.Write(
                dialog.FileName,
                SafeSheetName(form.Name),
                spec.Headers,
                rows,
                "xlsx");
            Notice = $"Đã tạo mẫu Excel trống: {form.Name}.";
        }
        catch (Exception exception)
        {
            Notice = "Không tạo được mẫu Excel.";
            MessageBox.Show(
                Window.GetWindow(this),
                string.IsNullOrWhiteSpace(exception.Message) ? Notice : exception.Message,
                "Biểu mẫu văn phòng",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void PreviewPdf_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: OfficeFormDefinition { Pdf: not null } form }) return;
        try
        {
            OfficeFormPrintPreview.Show(Window.GetWindow(this), form);
            Notice = $"Đã mở bản xem trước: {form.Name}.";
        }
        catch (Exception exception)
        {
            Notice = "Không mở được bản xem trước.";
            MessageBox.Show(
                Window.GetWindow(this),
                string.IsNullOrWhiteSpace(exception.Message) ? Notice : exception.Message,
                "Biểu mẫu văn phòng",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private static string Normalize(string value)
    {
        var source = (value ?? string.Empty).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(source.Length);
        foreach (var character in source)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(character));
        }
        return builder.ToString().Normalize(NormalizationForm.FormC).Trim();
    }

    private static string SafeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(character => invalid.Contains(character) ? '-' : character).ToArray());
        return string.Join(" ", cleaned.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string SafeSheetName(string value)
    {
        var cleaned = value.Replace(":", " -", StringComparison.Ordinal)
            .Replace("/", " - ", StringComparison.Ordinal)
            .Replace("\\", " - ", StringComparison.Ordinal)
            .Replace("*", string.Empty, StringComparison.Ordinal)
            .Replace("?", string.Empty, StringComparison.Ordinal)
            .Replace("[", "(", StringComparison.Ordinal)
            .Replace("]", ")", StringComparison.Ordinal);
        return cleaned.Length <= 31 ? cleaned : cleaned[..31].Trim();
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
