using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using NCalc;
using System;
using System.Threading.Tasks;
using System.Linq;


namespace NUMMET
{
    public sealed partial class RootFindingPage : Page
    {
        public RootFindingPage()
        {
            this.InitializeComponent();
            Button_Submit.Click += Button_Submit_Click;
            TextBox_Equation.IsEnabled = false;
            TextBox_EquationDerivative.IsEnabled = false;
            TextBox_Guess1.IsEnabled = false;
            TextBox_Guess2.IsEnabled = false;
            TextBox_PercentError.IsEnabled = false;
        }

        private void DisableTextBoxesForMethod(string method)
        {
            TextBox_Equation.IsEnabled = false;
            TextBox_EquationDerivative.IsEnabled = false;
            TextBox_Guess1.IsEnabled = false;
            TextBox_Guess2.IsEnabled = false;
            TextBox_PercentError.IsEnabled = false;

            if (method == "Bisection")
            {
                TextBox_Equation.IsEnabled = true;
                TextBox_Guess1.IsEnabled = true;
                TextBox_Guess2.IsEnabled = true;
                TextBox_PercentError.IsEnabled = true;
            }
            else if (method == "Newton-Raphson")
            {
                TextBox_Equation.IsEnabled = true;
                TextBox_EquationDerivative.IsEnabled = true;
                TextBox_Guess1.IsEnabled = true;
                TextBox_PercentError.IsEnabled = true;
            }
            else if (method == "Secant")
            {
                TextBox_Equation.IsEnabled = true;
                TextBox_Guess1.IsEnabled = true;
                TextBox_Guess2.IsEnabled = true;
                TextBox_PercentError.IsEnabled = true;
            }
        }

        private void Methods_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Methods.SelectedItem != null)
            {
                string selectedMethod = ((ComboBoxItem)Methods.SelectedItem).Content.ToString();
                DisableTextBoxesForMethod(selectedMethod);
            }
            else
            {
                DisableTextBoxesForMethod("");
            }
        }

        private void Button_Clear_Click(object sender, RoutedEventArgs e)
        {
            Methods.SelectedItem = null;

            TextBox_Equation.Text = string.Empty;
            TextBox_EquationDerivative.Text = string.Empty;
            TextBox_Guess1.Text = string.Empty;
            TextBox_Guess2.Text = string.Empty;
            TextBox_PercentError.Text = string.Empty;

            Methods.SelectedIndex = -1;

            TextBlock_Solution.Text = string.Empty;
        }

        private async void Button_Submit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string selectedMethod = (Methods.SelectedItem as ComboBoxItem)?.Content.ToString();

                switch (selectedMethod)
                {
                    case "Bisection":
                        await RunBisectionMethod();
                        break;
                    case "Newton-Raphson":
                        await RunNewtonRaphsonMethod();
                        break;
                    case "Secant":
                        await RunSecantMethod();
                        break;
                }
            }
            catch (Exception ex)
            {
                TextBlock_Solution.Text = $"Error: {ex.Message}";
            }
        }

        private async Task RunBisectionMethod()
        {
            try
            {
                string fx = TextBox_Equation.Text;
                double a = double.Parse(TextBox_Guess1.Text);
                double b = double.Parse(TextBox_Guess2.Text);
                double es = double.Parse(TextBox_PercentError.Text);

                double fa = EvaluateFunction(fx, a);
                double fb = EvaluateFunction(fx, b);

                if (fa * fb > 0)
                {
                    TextBlock_Solution.Text = "f(a) and f(b) must have opposite signs.";
                    return;
                }

                double c = 0, fc, prevC = 0, ea = 100;
                int iteration = 0;
                string output = $"{"Iter",-8} {"a",-16} {"b",-16} {"c",-16} {"f(c)",-16} {"ea (%)",-8}\n";

                do
                {
                    prevC = c;
                    c = (a + b) / 2;
                    fc = EvaluateFunction(fx, c);

                    if (iteration > 0)
                        ea = Math.Abs((c - prevC) / c) * 100;

                    output += $"{++iteration,-8} {a,-16:F4} {b,-16:F4} {c,-16:F4} {fc,-16:F4} {ea,-8:F2}\n";

                    if (fa * fc < 0)
                    {
                        b = c;
                        fb = fc;
                    }
                    else
                    {
                        a = c;
                        fa = fc;
                    }
                }
                while (ea > es);

                output += $"\nAPPROXIMATE ROOT: {c:F4}";
                await TypeOutSolution(output);
            }
            catch (Exception ex)
            {
                TextBlock_Solution.Text = $"Error: {ex.Message}";
            }
        }

        private async Task RunNewtonRaphsonMethod()
        {
            try
            {
                string fx = TextBox_Equation.Text;
                string dfx = TextBox_EquationDerivative.Text;
                double x0 = double.Parse(TextBox_Guess1.Text);
                double es = double.Parse(TextBox_PercentError.Text);
                double x1 = 0, ea = 100;
                int iteration = 0;

                string output = $"{"Iter",-8} {"x0",-16} {"f(x0)",-16} {"f'(x0)",-16} {"ea (%)",-8}\n";

                do
                {
                    double fxVal = EvaluateFunction(fx, x0);
                    double dfxVal = EvaluateFunction(dfx, x0);

                    if (Math.Abs(dfxVal) < 1e-12)
                    {
                        TextBlock_Solution.Text = "Derivative is too small. Newton-Raphson cannot proceed.";
                        return;
                    }

                    x1 = x0 - fxVal / dfxVal;

                    if (iteration > 0)
                        ea = Math.Abs((x1 - x0) / x1) * 100;

                    output += $"{++iteration,-8} {x0,-16:F4} {fxVal,-16:F4} {dfxVal,-16:F4} {ea,-8:F2}\n";

                    x0 = x1;
                    await Task.Delay(50);
                }
                while (ea > es);

                output += $"\nAPPROXIMATE ROOT: {x1:F4}";
                await TypeOutSolution(output);
            }
            catch (Exception ex)
            {
                TextBlock_Solution.Text = $"Error: {ex.Message}";
            }
        }

        private async Task RunSecantMethod()
        {
            try
            {
                string fx = TextBox_Equation.Text;
                double x0 = double.Parse(TextBox_Guess1.Text);  // First guess
                double x1 = double.Parse(TextBox_Guess2.Text);  // Second guess
                double es = double.Parse(TextBox_PercentError.Text);  // Desired percent error
                double ea = 100;  // Initial error
                int iteration = 0;

                string output = $"{"Iter",-8} {"x0",-16} {"x1",-16} {"f(x0)",-16} {"f(x1)",-16} {"ea (%)",-8}\n";

                do
                {
                    double fx0 = EvaluateFunction(fx, x0);  // f(x0)
                    double fx1 = EvaluateFunction(fx, x1);  // f(x1)

                    if (Math.Abs(fx1 - fx0) < 1e-12)  // Avoid division by a very small difference
                    {
                        TextBlock_Solution.Text = "The values are too close. Secant cannot proceed.";
                        return;
                    }

                    // Secant Method formula: x2 = x1 - f(x1) * (x1 - x0) / (f(x1) - f(x0))
                    double x2 = x1 - fx1 * (x1 - x0) / (fx1 - fx0);

                    if (iteration > 0)
                        ea = Math.Abs((x2 - x1) / x2) * 100;  // Percent error

                    output += $"{++iteration,-8} {x0,-16:F4} {x1,-16:F4} {fx0,-16:F4} {fx1,-16:F4} {ea,-8:F2}\n";

                    x0 = x1;  // Update x0
                    x1 = x2;  // Update x1
                    await Task.Delay(50);
                }
                while (ea > es);  // Continue until the error is within the desired tolerance

                output += $"\nAPPROXIMATE ROOT: {x1:F4}";
                await TypeOutSolution(output);  // Display the result
            }
            catch (Exception ex)
            {
                TextBlock_Solution.Text = $"Error: {ex.Message}";
            }
        }

        private async Task TypeOutSolution(string output)
        {
            TextBlock_Solution.Inlines.Clear();

            foreach (string line in output.Split('\n'))
            {
                if (line.StartsWith("APPROXIMATE ROOT:"))
                {
                    TextBlock_Solution.Inlines.Add(new Run { Text = "APPROXIMATE ROOT: " });
                    TextBlock_Solution.Inlines.Add(new Run
                    {
                        Text = line.Replace("APPROXIMATE ROOT: ", ""),
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold
                    });
                }
                else
                {
                    TextBlock_Solution.Inlines.Add(new Run { Text = line });
                }

                TextBlock_Solution.Inlines.Add(new LineBreak());
                await Task.Delay(100);
            }
        }

        private double EvaluateFunction(string expression, double x)
        {
            var e = new Expression(expression);
            e.Parameters["x"] = x;

            e.EvaluateFunction += (name, args) =>
            {
                try
                {
                    double a = Convert.ToDouble(args.Parameters[0].Evaluate());
                    double b = args.Parameters.Count() > 1 ? Convert.ToDouble(args.Parameters[1].Evaluate()) : 0;

                    args.Result = name.ToLower() switch
                    {
                        "pow" => (a == 0 && b == 0) ? double.NaN : Math.Pow(a, b),
                        "ln" => a <= 0 ? double.NaN : Math.Log(a),
                        "log" => a <= 0 ? double.NaN : Math.Log10(a),
                        "sin" => Math.Sin(a),
                        "cos" => Math.Cos(a),
                        "tan" => Math.Tan(a),
                        "exp" => a > 700 ? double.PositiveInfinity :
                                 a < -700 ? 0 : Math.Exp(a),
                        _ => double.NaN
                    };
                }
                catch
                {
                    args.Result = double.NaN;
                }
            };

            e.EvaluateParameter += (name, args) =>
            {
                if (name.ToLower() == "e")
                    args.Result = Math.E;
            };

            try
            {
                return Convert.ToDouble(e.Evaluate());
            }
            catch
            {
                return double.NaN;
            }
        }

        private void Button_Back_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
                Frame.GoBack();
        }
    }
}
