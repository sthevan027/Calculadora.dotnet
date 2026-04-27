using System;
using System.Collections.Generic;
using System.Globalization;

namespace Calculadora;

public static class ExpressionEvaluator
{
    public static double Evaluate(string expr, AngleUnit angleUnit)
    {
        var tokens = Tokenize(expr);
        var rpn = ToRpn(tokens);
        return EvalRpn(rpn, angleUnit);
    }

    private enum TokType { Number, Op, Func, LParen, RParen, Const }

    private sealed record Tok(TokType Type, string Text, double? Number = null);

    private static List<Tok> Tokenize(string expr)
    {
        var s = (expr ?? "").Trim()
            .Replace('×', '*')
            .Replace('÷', '/')
            .Replace('−', '-')
            .Replace(',', '.');

        var tokens = new List<Tok>();
        var i = 0;
        while (i < s.Length)
        {
            var c = s[i];
            if (char.IsWhiteSpace(c)) { i++; continue; }

            if (c == '(') { tokens.Add(new Tok(TokType.LParen, "(")); i++; continue; }
            if (c == ')') { tokens.Add(new Tok(TokType.RParen, ")")); i++; continue; }

            if ("+-*/^%".IndexOf(c) >= 0)
            {
                // '%' aqui é percentual (unário, contextual) — não é "mod"
                var op = c == '%' ? "pct" : c.ToString();
                tokens.Add(new Tok(TokType.Op, op));
                i++;
                continue;
            }

            if (char.IsDigit(c) || c == '.')
            {
                var start = i;
                i++;
                while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.')) i++;
                var raw = s[start..i];
                if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                    throw new FormatException("Número inválido");
                tokens.Add(new Tok(TokType.Number, raw, v));
                continue;
            }

            if (char.IsLetter(c) || c == 'π')
            {
                var start = i;
                i++;
                while (i < s.Length && (char.IsLetter(s[i]) || s[i] == 'π')) i++;
                var id = s[start..i].ToLowerInvariant();

                if (id is "pi" or "π") { tokens.Add(new Tok(TokType.Const, "pi", Math.PI)); continue; }
                if (id is "e") { tokens.Add(new Tok(TokType.Const, "e", Math.E)); continue; }

                tokens.Add(new Tok(TokType.Func, id));
                continue;
            }

            throw new FormatException($"Caractere inválido: {c}");
        }

        return tokens;
    }

    private static int Prec(string op) => op switch
    {
        "u-" => 5,
        "pct" => 5,
        "^" => 4,
        "*" or "/" => 3,
        "+" or "-" => 2,
        _ => 0
    };

    private static bool RightAssoc(string op) => op is "^" or "u-" or "pct";

    private static List<Tok> ToRpn(List<Tok> tokens)
    {
        var output = new List<Tok>();
        var stack = new Stack<Tok>();

        Tok? prev = null;
        foreach (var t in tokens)
        {
            if (t.Type is TokType.Number or TokType.Const)
            {
                output.Add(t);
            }
            else if (t.Type == TokType.Func)
            {
                stack.Push(t);
            }
            else if (t.Type == TokType.Op)
            {
                var op = t.Text;
                if (op == "-" && (prev is null || prev.Type is TokType.Op or TokType.LParen))
                    op = "u-";

                var cur = new Tok(TokType.Op, op);

                while (stack.Count > 0 && stack.Peek().Type is TokType.Op or TokType.Func)
                {
                    var top = stack.Peek();
                    if (top.Type == TokType.Func)
                    {
                        output.Add(stack.Pop());
                        continue;
                    }

                    var pTop = Prec(top.Text);
                    var pCur = Prec(cur.Text);
                    if (pTop > pCur || (pTop == pCur && !RightAssoc(cur.Text)))
                        output.Add(stack.Pop());
                    else break;
                }

                stack.Push(cur);
            }
            else if (t.Type == TokType.LParen)
            {
                stack.Push(t);
            }
            else if (t.Type == TokType.RParen)
            {
                while (stack.Count > 0 && stack.Peek().Type != TokType.LParen)
                    output.Add(stack.Pop());
                if (stack.Count == 0) throw new FormatException("Parênteses inválidos");
                _ = stack.Pop();
                if (stack.Count > 0 && stack.Peek().Type == TokType.Func)
                    output.Add(stack.Pop());
            }

            prev = t;
        }

        while (stack.Count > 0)
        {
            var t = stack.Pop();
            if (t.Type is TokType.LParen or TokType.RParen) throw new FormatException("Parênteses inválidos");
            output.Add(t);
        }

        return output;
    }

    private readonly record struct Num(double Value, bool IsPercent);

    private static double EvalRpn(List<Tok> rpn, AngleUnit angleUnit)
    {
        var st = new Stack<Num>();
        foreach (var t in rpn)
        {
            if (t.Type is TokType.Number or TokType.Const)
            {
                st.Push(new Num(t.Number!.Value, false));
                continue;
            }

            if (t.Type == TokType.Op)
            {
                if (t.Text == "u-")
                {
                    if (st.Count < 1) throw new FormatException("Expressão inválida");
                    var x = st.Pop();
                    st.Push(new Num(-x.Value, false));
                    continue;
                }

                if (t.Text == "pct")
                {
                    if (st.Count < 1) throw new FormatException("Expressão inválida");
                    var x = st.Pop();
                    st.Push(new Num(x.Value / 100.0, true));
                    continue;
                }

                if (st.Count < 2) throw new FormatException("Expressão inválida");
                var b = st.Pop();
                var a = st.Pop();

                // Percentual contextual:
                // - Em + e -: "A + B%" => A + (A * (B/100))
                // - Em * e /: "A * B%" => A * (B/100)
                var rhs = b.IsPercent && (t.Text == "+" || t.Text == "-") ? a.Value * b.Value : b.Value;

                var result = t.Text switch
                {
                    "+" => a.Value + rhs,
                    "-" => a.Value - rhs,
                    "*" => a.Value * rhs,
                    "/" => rhs == 0 ? double.NaN : a.Value / rhs,
                    "^" => Math.Pow(a.Value, rhs),
                    _ => throw new FormatException("Operador inválido")
                };

                st.Push(new Num(result, false));
                continue;
            }

            if (t.Type == TokType.Func)
            {
                if (st.Count < 1) throw new FormatException("Expressão inválida");
                var x0 = st.Pop().Value;
                var xRad = angleUnit == AngleUnit.Degrees ? x0 * (Math.PI / 180.0) : x0;

                double result = t.Text switch
                {
                    "sin" => Math.Sin(xRad),
                    "cos" => Math.Cos(xRad),
                    "tan" => Math.Tan(xRad),
                    "asin" => angleUnit == AngleUnit.Degrees ? Math.Asin(x0) * (180.0 / Math.PI) : Math.Asin(x0),
                    "acos" => angleUnit == AngleUnit.Degrees ? Math.Acos(x0) * (180.0 / Math.PI) : Math.Acos(x0),
                    "atan" => angleUnit == AngleUnit.Degrees ? Math.Atan(x0) * (180.0 / Math.PI) : Math.Atan(x0),
                    "sqrt" => Math.Sqrt(x0),
                    "abs" => Math.Abs(x0),
                    "ln" => Math.Log(x0),
                    "log" => Math.Log10(x0),
                    "exp" => Math.Exp(x0),
                    _ => throw new FormatException("Função inválida")
                };

                st.Push(new Num(result, false));
                continue;
            }
        }

        if (st.Count != 1) throw new FormatException("Expressão inválida");
        return st.Pop().Value;
    }
}

