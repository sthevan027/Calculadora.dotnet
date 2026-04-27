using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Calculadora;

public partial class MainWindow : Window
{
    private CalculatorMode _mode = CalculatorMode.Basic;
    private string _sciExpr = "";
    private readonly System.Collections.Generic.List<HistoryEntry> _history = new();
    private AngleUnit _angleUnit = AngleUnit.Degrees;

    private string _entry = "0";
    private double? _accumulator;
    private string? _pendingOp;
    private bool _replaceEntry = true;

    public MainWindow()
    {
        InitializeComponent();
        UpdateDisplay();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        ApplySettings();
        Focus();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2) return;
        DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var w = new SettingsWindow
        {
            Owner = this,
            Width = ActualWidth,
            Height = ActualHeight
        };
        w.ShowDialog();
        ApplySettings();
        Focus();
    }

    private void Digit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b) return;
        AppendDigit(b.Content?.ToString() ?? "");
    }

    private void Decimal_Click(object sender, RoutedEventArgs e)
    {
        if (_mode == CalculatorMode.Scientific && _replaceEntry && _sciExpr.Length > 0 && _sciExpr.TrimEnd().EndsWith('%'))
        {
            // evita "10%,"
            return;
        }
        if (_replaceEntry)
        {
            _entry = "0,";
            _replaceEntry = false;
            UpdateDisplay();
            return;
        }

        if (!_entry.Contains(','))
        {
            _entry += ",";
            UpdateDisplay();
        }
    }

    private void PlusMinus_Click(object sender, RoutedEventArgs e)
    {
        if (_entry == "0") return;
        _entry = _entry.StartsWith('-') ? _entry[1..] : "-" + _entry;
        _replaceEntry = false;
        UpdateDisplay();
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        _entry = "0";
        _accumulator = null;
        _pendingOp = null;
        _replaceEntry = true;
        _sciExpr = "";
        ExpressionText.Text = "";
        UpdateDisplay();
    }

    private void Backspace_Click(object sender, RoutedEventArgs e)
    {
        if (_mode == CalculatorMode.Scientific && _replaceEntry)
        {
            if (_sciExpr.Length > 0)
            {
                _sciExpr = _sciExpr[..^1];
                ExpressionText.Text = _sciExpr;
            }
            return;
        }
        if (_replaceEntry) return;
        if (_entry.Length <= 1 || (_entry.Length == 2 && _entry.StartsWith('-')))
        {
            _entry = "0";
            _replaceEntry = true;
        }
        else
        {
            _entry = _entry[..^1];
        }
        UpdateDisplay();
    }

    private void Percent_Click(object sender, RoutedEventArgs e)
    {
        if (_mode == CalculatorMode.Scientific)
        {
            // Em científica, % vira operador de percentual na expressão (contextual)
            if (!_replaceEntry)
            {
                _sciExpr = (_sciExpr + " " + _entry.Replace(',', '.') + " ").Trim();
                _replaceEntry = true;
            }

            _sciExpr = (_sciExpr + " %").Trim();
            ExpressionText.Text = _sciExpr;
            return;
        }
        var current = ParseEntry();
        if (_accumulator is null || _pendingOp is null)
        {
            SetEntry(current / 100.0);
            return;
        }

        SetEntry(_accumulator.Value * (current / 100.0));
    }

    private void Op_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b) return;
        var op = (b.Tag?.ToString() ?? "").Trim();
        if (string.IsNullOrWhiteSpace(op)) return;

        if (_mode == CalculatorMode.Scientific)
        {
            SciAppendOperator(op);
            return;
        }

        if (_pendingOp is not null && !_replaceEntry)
        {
            Evaluate();
        }

        _accumulator ??= ParseEntry();
        _pendingOp = op;
        _replaceEntry = true;
        ExpressionText.Text = $"{FormatNumber(_accumulator.Value)} {PrettyOp(_pendingOp)}";
        UpdateDisplay();
    }

    private void Equals_Click(object sender, RoutedEventArgs e) => Evaluate();

    private void Evaluate()
    {
        if (_mode == CalculatorMode.Scientific)
        {
            SciEvaluate();
            return;
        }
        if (_pendingOp is null || _accumulator is null)
        {
            _replaceEntry = true;
            UpdateDisplay();
            return;
        }

        var rhs = ParseEntry();
        var lhs = _accumulator.Value;

        double result = _pendingOp switch
        {
            "+" => lhs + rhs,
            "-" => lhs - rhs,
            "*" => lhs * rhs,
            "/" => rhs == 0 ? double.NaN : lhs / rhs,
            _ => rhs
        };

        ExpressionText.Text = $"{FormatNumber(lhs)} {PrettyOp(_pendingOp)} {FormatNumber(rhs)} =";
        _pendingOp = null;
        _accumulator = null;
        _replaceEntry = true;
        SetEntry(result);
    }

    private void Func_Click(object sender, RoutedEventArgs e)
    {
        if (_mode != CalculatorMode.Scientific) return;
        if (sender is not Button b) return;
        var fn = (b.Tag?.ToString() ?? "").Trim();
        if (string.IsNullOrWhiteSpace(fn)) return;
        SciAppendFunction(fn);
    }

    private void Const_Click(object sender, RoutedEventArgs e)
    {
        if (_mode != CalculatorMode.Scientific) return;
        if (sender is not Button b) return;
        var c = (b.Tag?.ToString() ?? "").Trim();
        if (string.IsNullOrWhiteSpace(c)) return;
        SciAppendConst(c);
    }

    private void Paren_Click(object sender, RoutedEventArgs e)
    {
        if (_mode != CalculatorMode.Scientific) return;
        if (sender is not Button b) return;
        var p = (b.Tag?.ToString() ?? "").Trim();
        if (p is not "(" and not ")") return;
        SciAppendParen(p);
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (_mode == CalculatorMode.Scientific)
        {
            if (e.Key == Key.D9 && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                SciAppendParen("(");
                e.Handled = true;
                return;
            }
            if (e.Key == Key.D0 && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                SciAppendParen(")");
                e.Handled = true;
                return;
            }
        }

        if (e.Key is >= Key.D0 and <= Key.D9)
        {
            AppendDigit(((int)(e.Key - Key.D0)).ToString());
            e.Handled = true;
            return;
        }

        if (e.Key is >= Key.NumPad0 and <= Key.NumPad9)
        {
            AppendDigit(((int)(e.Key - Key.NumPad0)).ToString());
            e.Handled = true;
            return;
        }

        switch (e.Key)
        {
            case Key.Add:
            case Key.OemPlus when Keyboard.Modifiers.HasFlag(ModifierKeys.Shift):
                SetOpFromKeyboard("+");
                e.Handled = true;
                break;
            case Key.Subtract:
            case Key.OemMinus:
                SetOpFromKeyboard("-");
                e.Handled = true;
                break;
            case Key.Multiply:
                SetOpFromKeyboard("*");
                e.Handled = true;
                break;
            case Key.Divide:
            case Key.Oem2:
                SetOpFromKeyboard("/");
                e.Handled = true;
                break;
            case Key.Enter:
                Evaluate();
                e.Handled = true;
                break;
            case Key.Decimal:
            case Key.OemComma:
            case Key.OemPeriod:
                Decimal_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;
            case Key.Oem5: // geralmente '%' em teclados ABNT pode variar, mas cobre alguns layouts
                if (_mode == CalculatorMode.Scientific)
                {
                    Percent_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
                break;
            case Key.Back:
                Backspace_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;
            case Key.Delete:
            case Key.Escape:
                Clear_Click(this, new RoutedEventArgs());
                e.Handled = true;
                break;
        }
    }

    private void SetOpFromKeyboard(string op)
    {
        if (_mode == CalculatorMode.Scientific)
        {
            SciAppendOperator(op);
            return;
        }
        if (_pendingOp is not null && !_replaceEntry)
        {
            Evaluate();
        }

        _accumulator ??= ParseEntry();
        _pendingOp = op;
        _replaceEntry = true;
        ExpressionText.Text = $"{FormatNumber(_accumulator.Value)} {PrettyOp(_pendingOp)}";
        UpdateDisplay();
    }

    private void AppendDigit(string digit)
    {
        if (digit.Length != 1 || digit[0] < '0' || digit[0] > '9') return;

        if (_replaceEntry)
        {
            _entry = digit;
            _replaceEntry = false;
            if (_mode == CalculatorMode.Scientific)
            {
                ExpressionText.Text = _sciExpr;
            }
            UpdateDisplay();
            return;
        }

        if (_entry == "0") _entry = digit;
        else _entry += digit;
        UpdateDisplay();
    }

    private double ParseEntry()
    {
        if (double.TryParse(_entry.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v))
            return v;
        return 0;
    }

    private void SetEntry(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            _entry = "Erro";
            _replaceEntry = true;
            UpdateDisplay();
            return;
        }

        _entry = FormatNumber(value);
        _replaceEntry = true;
        UpdateDisplay();
    }

    private static string FormatNumber(double value)
    {
        var s = value.ToString("0.###############", System.Globalization.CultureInfo.InvariantCulture);
        return s.Replace('.', ',');
    }

    private static string PrettyOp(string? op) => op switch
    {
        "+" => "+",
        "-" => "−",
        "*" => "×",
        "/" => "÷",
        _ => op ?? ""
    };

    private void UpdateDisplay()
    {
        DisplayText.Text = _entry;
    }

    private void ApplySettings()
    {
        _mode = App.Settings.Mode;
        _angleUnit = App.Settings.AngleUnit;
        ModeBadge.Text = _mode == CalculatorMode.Scientific ? "Científica" : "Básica";

        var sciVisible = _mode == CalculatorMode.Scientific ? Visibility.Visible : Visibility.Collapsed;
        ScientificRow1.Visibility = sciVisible;
        ScientificRow2.Visibility = sciVisible;
        ScientificRow3.Visibility = sciVisible;

        HistoryPanel.Visibility = App.Settings.HistoryEnabled ? Visibility.Visible : Visibility.Collapsed;
        Width = App.Settings.HistoryEnabled ? (_mode == CalculatorMode.Scientific ? 640 : 610) : 420;
        Height = _mode == CalculatorMode.Scientific ? 720 : 680;

        if (App.Settings.HistoryEnabled)
        {
            if (_history.Count == 0)
            {
                _history.AddRange(HistoryStore.Load());
            }
            RenderHistory();
        }
    }

    private void RenderHistory()
    {
        if (!App.Settings.HistoryEnabled) return;
        HistoryList.Items.Clear();
        for (var i = _history.Count - 1; i >= 0; i--)
        {
            var h = _history[i];
            HistoryList.Items.Add($"{h.Expression} = {h.Result}");
        }
    }

    private void AddHistory(string expression, string result)
    {
        if (!App.Settings.HistoryEnabled) return;
        if (string.IsNullOrWhiteSpace(expression)) return;
        _history.Add(new HistoryEntry(DateTimeOffset.Now, expression, result));
        while (_history.Count > 60) _history.RemoveAt(0);
        HistoryStore.Save(_history);
        RenderHistory();
    }

    private void ClearHistory_Click(object sender, RoutedEventArgs e)
    {
        _history.Clear();
        HistoryStore.Save(_history);
        RenderHistory();
    }

    private void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (HistoryList.SelectedItem is not string s) return;
        var idx = s.LastIndexOf("= ", StringComparison.Ordinal);
        if (idx <= 0) return;
        var result = s[(idx + 2)..].Trim();
        _entry = result;
        _replaceEntry = true;
        UpdateDisplay();
        HistoryList.SelectedItem = null;
    }

    // ---------- Scientific mode helpers ----------
    private void SciAppendOperator(string op)
    {
        if (!_replaceEntry)
        {
            _sciExpr = (_sciExpr + " " + _entry.Replace(',', '.') + " ").Trim();
            _replaceEntry = true;
        }

        if (_sciExpr.Length == 0)
        {
            _sciExpr = _entry.Replace(',', '.');
        }

        _sciExpr = _sciExpr.TrimEnd();
        if (_sciExpr.EndsWith("+") || _sciExpr.EndsWith("-") || _sciExpr.EndsWith("*") || _sciExpr.EndsWith("/") || _sciExpr.EndsWith("^"))
        {
            _sciExpr = _sciExpr[..^1] + op;
        }
        else
        {
            _sciExpr += " " + op;
        }

        ExpressionText.Text = _sciExpr;
    }

    private void SciAppendFunction(string fn)
    {
        if (!_replaceEntry)
        {
            _sciExpr = (_sciExpr + " " + _entry.Replace(',', '.') + " ").Trim();
            _replaceEntry = true;
        }
        _sciExpr = (_sciExpr + " " + fn + "(").Trim();
        ExpressionText.Text = _sciExpr;
    }

    private void SciAppendConst(string c)
    {
        if (_replaceEntry)
        {
            _entry = c == "pi" ? "3,14159265358979" : "2,718281828459045";
            _replaceEntry = false;
            UpdateDisplay();
            return;
        }

        _entry += c == "pi" ? "3,14159265358979" : "2,718281828459045";
        UpdateDisplay();
    }

    private void SciAppendParen(string p)
    {
        if (!_replaceEntry)
        {
            _sciExpr = (_sciExpr + " " + _entry.Replace(',', '.') + " ").Trim();
            _replaceEntry = true;
        }
        _sciExpr = (_sciExpr + " " + p).Trim();
        ExpressionText.Text = _sciExpr;
    }

    private void SciEvaluate()
    {
        var expr = _sciExpr;
        if (!_replaceEntry)
        {
            expr = (expr + " " + _entry.Replace(',', '.')).Trim();
        }

        if (string.IsNullOrWhiteSpace(expr))
        {
            _replaceEntry = true;
            UpdateDisplay();
            return;
        }

        try
        {
            var v = ExpressionEvaluator.Evaluate(expr, _angleUnit);
            var result = FormatNumber(v);
            ExpressionText.Text = expr.Replace('.', ',') + " =";
            _sciExpr = "";
            _entry = result;
            _replaceEntry = true;
            UpdateDisplay();
            AddHistory(expr.Replace('.', ','), result);
        }
        catch
        {
            _entry = "Erro";
            _replaceEntry = true;
            UpdateDisplay();
        }
    }
}