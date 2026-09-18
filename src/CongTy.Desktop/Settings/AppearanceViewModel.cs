using System.ComponentModel;
using System.Runtime.CompilerServices;
using CongTy.Desktop.Themes;
using CongTy.Windows;

namespace CongTy.Desktop.Settings;

public sealed record AppearanceThemeOption(string Key, string Label, string Description)
{
    public override string ToString() => Label;
}

public sealed class AppearanceViewModel : INotifyPropertyChanged
{
    private readonly ILocalSettingsStore _settingsStore;
    private readonly DesktopSettingsState _settingsState;
    private AppearanceThemeOption _selectedTheme;
    private int _scale;
    private bool _isBusy;
    private string _message = string.Empty;
    private bool _messageIsError;

    public AppearanceViewModel(ILocalSettingsStore settingsStore, DesktopSettingsState settingsState)
    {
        _settingsStore = settingsStore;
        _settingsState = settingsState;
        _selectedTheme = ThemeOptions.First(item => item.Key == ThemeManager.ToAppearanceKey(settingsState.Current.Theme));
        _scale = ThemeManager.NormalizeScale(settingsState.Current.DisplayScale);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<AppearanceThemeOption> ThemeOptions { get; } =
    [
        new("default", "Mặc định", "Giữ nguyên giao diện Công Ty hiện tại."),
        new("green", "Xanh lá", "Nền nội dung trắng, thanh điều hướng và điểm nhấn dùng hệ xanh lá."),
        new("dark", "Tối", "Nền tối đồng bộ cho nội dung, bảng, trường nhập và thanh điều hướng.")
    ];

    public IReadOnlyList<int> ScaleLevels { get; } = [-4, -3, -2, -1, 0, 1, 2, 3, 4];

    public AppearanceThemeOption SelectedTheme
    {
        get => _selectedTheme;
        private set => Set(ref _selectedTheme, value, nameof(SelectedTheme), nameof(CurrentThemeLabel), nameof(IsDefaultTheme), nameof(IsGreenTheme), nameof(IsDarkTheme));
    }

    public int Scale
    {
        get => _scale;
        private set => Set(ref _scale, value, nameof(Scale), nameof(ScaleLabel), nameof(CanResetScale));
    }

    public string CurrentThemeLabel => SelectedTheme.Label;
    public bool IsDefaultTheme => SelectedTheme.Key == "default";
    public bool IsGreenTheme => SelectedTheme.Key == "green";
    public bool IsDarkTheme => SelectedTheme.Key == "dark";
    public string ScaleLabel => Scale == 0 ? "Mặc định" : Scale > 0 ? $"Lớn hơn {Scale} cấp" : $"Nhỏ hơn {Math.Abs(Scale)} cấp";
    public bool CanResetScale => Scale != 0 && !IsBusy;
    public bool IsBusy { get => _isBusy; private set => Set(ref _isBusy, value, nameof(IsBusy), nameof(CanResetScale)); }
    public string Message { get => _message; private set => Set(ref _message, value, nameof(Message), nameof(HasMessage)); }
    public bool HasMessage => !string.IsNullOrWhiteSpace(Message);
    public bool MessageIsError { get => _messageIsError; private set => Set(ref _messageIsError, value); }

    public async Task ChooseThemeAsync(string key)
    {
        var option = ThemeOptions.FirstOrDefault(item => string.Equals(item.Key, key, StringComparison.Ordinal));
        if (option is null || IsBusy) return;

        SelectedTheme = option;
        ThemeManager.Apply(ThemeManager.FromAppearanceKey(option.Key));
        await PersistAsync();
    }

    public void PreviewScale(int scale)
    {
        Scale = ThemeManager.NormalizeScale(scale);
        ThemeManager.ApplyScale(Scale);
    }

    public async Task CommitScaleAsync()
    {
        if (IsBusy) return;
        ThemeManager.ApplyScale(Scale);
        await PersistAsync();
    }

    public async Task ResetScaleAsync()
    {
        PreviewScale(0);
        await CommitScaleAsync();
    }

    private async Task PersistAsync()
    {
        IsBusy = true;
        SetMessage(string.Empty, false);
        try
        {
            var next = _settingsState.Current with
            {
                Theme = ThemeManager.FromAppearanceKey(SelectedTheme.Key),
                DisplayScale = Scale
            };
            await _settingsStore.SaveAsync(next);
            _settingsState.Update(next);
            SetMessage("Đã lưu lựa chọn giao diện trên máy tính này.", false);
        }
        catch
        {
            SetMessage("Chưa thể lưu lựa chọn giao diện. Thay đổi vẫn được áp dụng trong phiên hiện tại.", true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetMessage(string message, bool isError)
    {
        MessageIsError = isError;
        Message = message;
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null, params string[] dependentProperties)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        foreach (var dependent in dependentProperties) OnPropertyChanged(dependent);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
