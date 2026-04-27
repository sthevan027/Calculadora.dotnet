using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Calculadora;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        SelectThemeItem(App.Settings.Theme);
        SelectModeItem(App.Settings.Mode);
        SelectAngleItem(App.Settings.AngleUnit);
        HistoryCheck.IsChecked = App.Settings.HistoryEnabled;
    }

    private void SelectThemeItem(AppTheme theme)
    {
        var tag = theme switch
        {
            AppTheme.Light => "Light",
            AppTheme.Dark => "Dark",
            _ => "System"
        };

        var item = ThemeCombo.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (i.Tag?.ToString() ?? "") == tag);
        if (item is not null) ThemeCombo.SelectedItem = item;
    }

    private void SelectModeItem(CalculatorMode mode)
    {
        var tag = mode switch
        {
            CalculatorMode.Scientific => "Scientific",
            _ => "Basic"
        };

        var item = ModeCombo.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (i.Tag?.ToString() ?? "") == tag);
        if (item is not null) ModeCombo.SelectedItem = item;
    }

    private void SelectAngleItem(AngleUnit unit)
    {
        var tag = unit == AngleUnit.Radians ? "Radians" : "Degrees";
        var item = AngleCombo.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (i.Tag?.ToString() ?? "") == tag);
        if (item is not null) AngleCombo.SelectedItem = item;
    }

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        if (ThemeCombo.SelectedItem is not ComboBoxItem item) return;

        var theme = (item.Tag?.ToString() ?? "System") switch
        {
            "Light" => AppTheme.Light,
            "Dark" => AppTheme.Dark,
            _ => AppTheme.System
        };

        App.Settings.Theme = theme;
        SettingsStore.Save(App.Settings);
        ThemeManager.Apply(theme);
    }

    private void ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        if (ModeCombo.SelectedItem is not ComboBoxItem item) return;

        var mode = (item.Tag?.ToString() ?? "Basic") switch
        {
            "Scientific" => CalculatorMode.Scientific,
            _ => CalculatorMode.Basic
        };

        App.Settings.Mode = mode;
        SettingsStore.Save(App.Settings);
    }

    private void AngleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        if (AngleCombo.SelectedItem is not ComboBoxItem item) return;

        var unit = (item.Tag?.ToString() ?? "Degrees") switch
        {
            "Radians" => AngleUnit.Radians,
            _ => AngleUnit.Degrees
        };

        App.Settings.AngleUnit = unit;
        SettingsStore.Save(App.Settings);
    }

    private void HistoryCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        App.Settings.HistoryEnabled = HistoryCheck.IsChecked == true;
        SettingsStore.Save(App.Settings);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}

