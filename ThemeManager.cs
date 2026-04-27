using System;
using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace Calculadora;

public static class ThemeManager
{
    public static AppTheme CurrentTheme { get; private set; } = AppTheme.System;

    public static void Apply(AppTheme theme)
    {
        CurrentTheme = theme;
        var resolved = theme == AppTheme.System ? GetSystemTheme() : theme;
        var source = resolved == AppTheme.Light ? "Themes/Light.xaml" : "Themes/Dark.xaml";

        if (Application.Current.Resources.MergedDictionaries.Count == 0)
        {
            Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(source, UriKind.Relative) });
            return;
        }

        var idx = FindThemeDictionaryIndex();
        var dict = new ResourceDictionary { Source = new Uri(source, UriKind.Relative) };
        if (idx >= 0) Application.Current.Resources.MergedDictionaries[idx] = dict;
        else Application.Current.Resources.MergedDictionaries.Insert(0, dict);
    }

    private static int FindThemeDictionaryIndex()
    {
        for (var i = 0; i < Application.Current.Resources.MergedDictionaries.Count; i++)
        {
            var src = Application.Current.Resources.MergedDictionaries[i].Source?.OriginalString ?? "";
            if (src.Contains("Themes/Light.xaml", StringComparison.OrdinalIgnoreCase) ||
                src.Contains("Themes/Dark.xaml", StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private static AppTheme GetSystemTheme()
    {
        try
        {
            var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var appsUseLight = key?.GetValue("AppsUseLightTheme");
            if (appsUseLight is int v) return v == 0 ? AppTheme.Dark : AppTheme.Light;
        }
        catch
        {
            // ignore
        }
        return AppTheme.Dark;
    }
}

