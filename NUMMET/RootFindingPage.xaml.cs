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
        }

        private void Button_Clear_Click(object sender, RoutedEventArgs e)
        {
            // Clear all the input fields
            TextBox_Equation.Text = string.Empty;
            TextBox_EquationDerivative.Text = string.Empty;
            TextBox_Guess1.Text = string.Empty;
            TextBox_Guess2.Text = string.Empty;
            TextBox_PercentError.Text = string.Empty;

            // Optionally, clear the method selection
            Methods.SelectedIndex = -1;

            // Clear the solution area
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
                    default:
                        TextBlock_Solution.Text = "Only Bisection and Newton-Raphson are implemented.";
                        break;
                }
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
