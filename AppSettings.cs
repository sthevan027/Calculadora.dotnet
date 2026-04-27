namespace Calculadora;

public enum AppTheme
{
    System = 0,
    Light = 1,
    Dark = 2
}

public enum CalculatorMode
{
    Basic = 0,
    Scientific = 1
}

public enum AngleUnit
{
    Degrees = 0,
    Radians = 1
}

public sealed class AppSettings
{
    public AppTheme Theme { get; set; } = AppTheme.System;
    public CalculatorMode Mode { get; set; } = CalculatorMode.Basic;
    public bool HistoryEnabled { get; set; } = true;
    public AngleUnit AngleUnit { get; set; } = AngleUnit.Degrees;
}

