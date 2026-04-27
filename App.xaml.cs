using System.Windows;

namespace Calculadora;

public partial class App : Application
{
    public static AppSettings Settings { get; private set; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Settings = SettingsStore.Load();
        ThemeManager.Apply(Settings.Theme);
    }
}

