namespace LogicAnalyzer.Controls;

using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

using NCalc;

public class ExpressionNumericUpDown : NumericUpDown
{
    protected override Type StyleKeyOverride => typeof(NumericUpDown);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key != Key.Enter || EvaluateExpression()) {
            base.OnKeyDown(e);
        }
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        if (EvaluateExpression()) {
            base.OnLostFocus(e);
        }
    }

    private bool EvaluateExpression()
    {
        var text = base.Text ?? string.Empty;

        try {
            var expression = new Expression(text.Replace(",", ""));
            var result = Convert.ToDecimal(expression.Evaluate());
            base.BorderBrush = null;
            base.Value = Math.Clamp(result, base.Minimum, base.Maximum);
            return true;
        } catch {
            base.BorderBrush = Brushes.Red;
            base.Value = null;
            base.Text = text;
            return false;
        }
    }
}
