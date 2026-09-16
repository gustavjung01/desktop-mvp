namespace CongTy.Windows;

public sealed class DesktopSettingsState(DesktopSettings initialSettings)
{
    public DesktopSettings Current { get; private set; } =
        initialSettings ?? throw new ArgumentNullException(nameof(initialSettings));

    public event EventHandler<DesktopSettings>? Changed;

    public void Update(DesktopSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Current = settings;
        Changed?.Invoke(this, settings);
    }
}
