using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using NCalc;
using System.Text;
using Microsoft.UI.Xaml.Documents;
using System.Threading.Tasks;

namespace NUMMET
{
    public sealed partial class IntegrationPage : Page
    {
        public IntegrationPage()
        {
            this.InitializeComponent();
            Button_Solve.Click += Button_Solve_Click;
            Button_Clear.Click += Button_Clear_Click;
        }

        private async void Button_Solve_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBox_Method.SelectedItem is ComboBoxItem selectedItem)
            {
                string method = selectedItem.Content.ToString();
                string expression = ExpressionInput.Text;
                double a, b;

                try
                {
                    a = EvaluateFunction(IntegrationLowerBound.Text, 0);
                    b = EvaluateFunction(IntegrationUpperBound.Text, 0);

                    if (double.IsNaN(a) || double.IsInfinity(a))
                    {
                        TextBlock_Solution.Text = "Error: Invalid or non-finite lower bound expression.";
                        return;
                    }
                    if (double.IsNaN(b) || double.IsInfinity(b))
                    {
                        TextBlock_Solution.Text = "Error: Invalid or non-finite upper bound expression.";
                        return;
                    }

                    if (method == "Trapezoidal")
                    {
                        if (!int.TryParse(Division_Number.Text, out int n) || n <= 0)
                        {
                            TextBlock_Solution.Text = "Error: Invalid number of trapezoids. Must be a positive integer.";
                            return;
                        }
                        string solutionSteps = TrapezoidalRule(expression, a, b, n);
                        await TypeOutSolution(solutionSteps);
                        TextBlock_Solution.Text = solutionSteps;
                    }
                    else if (method == "Simpson's 1/3")
                    {
                        if (!int.TryParse(Division_Number.Text, out int n) || n <= 0 || n % 2 != 0)
                        {
                            TextBlock_Solution.Text = "Error: Invalid number of divisions for Simpson's 1/3 Rule. Must be a positive even integer.";
                            return;
                        }
                        string solutionSteps = SimpsonsRule(expression, a, b, n);
                        await TypeOutSolution(solutionSteps);
                        TextBlock_Solution.Text = solutionSteps;
                    }
                    else
                    {
                        TextBlock_Solution.Text = "Error: Please select a valid method.";
                    }
                }
                catch (Exception ex)
                {
                    TextBlock_Solution.Text = $"Error: {ex.Message}";
                }
            }
        }

        private void Button_Clear_Click(object sender, RoutedEventArgs e)
        {
            TextBlock_Solution.Text = "";
            ExpressionInput.Text = "";
            IntegrationLowerBound.Text = "";
            IntegrationUpperBound.Text = "";
            Division_Number.Text = ""; // Clear trapezoid input (now handles "n" for both)
        }

        private string TrapezoidalRule(string expression, double a, double b, int n)
        {
            StringBuilder steps = new StringBuilder();
            double h = (b - a) / n;
            steps.AppendLine($"Applying Trapezoidal Rule with n = {n}, h = (b - a) / n = ({b} - {a}) / {n} = {h:F4}");
            steps.AppendLine($"\n────୨ৎ──── Step 1: Evaluate f(x) at the endpoints and intermediate points: ────୨ৎ────");
            double sum = 0;

            for (int i = 0; i <= n; i++)
            {
                double x = a + i * h;
                double y = EvaluateFunction(expression, x);
                if (double.IsNaN(y) || double.IsInfinity(y))
                {
                    throw new ArgumentException($"Function evaluation resulted in a non-finite value at x = {x}. Please check your function and bounds.");
                }
                steps.AppendLine($"  f(x_{i}) = f({x:F4}) = {y:F4}");
                if (i == 0 || i == n)
                    sum += y;
                else
                    sum += 2 * y;
            }

            steps.AppendLine($"\n────୨ৎ──── Step 2: Apply the Trapezoidal Rule formula: ────୨ৎ────");
            steps.AppendLine($"🌷 Integral ≈ (h / 2) * [f(x_0) + 2f(x_1) + ... + 2f(x_{n - 1}) + f(x_n)]");
            steps.AppendLine($"            ≈ ({h:F4} / 2) * [{sum:F4}]");
            double result = (h / 2) * sum;
            steps.AppendLine($"            ≈ {result:F4}");

            return steps.ToString();
        }

        // Modified SimpsonsRule to accept 'n' (number of divisions)
        private string SimpsonsRule(string expression, double a, double b, int n)
        {
            StringBuilder steps = new StringBuilder();
            if (n % 2 != 0)
            {
                throw new ArgumentException("The number of divisions for Simpson's 1/3 Rule must be an even integer.");
            }

            double h = (b - a) / n;
            steps.AppendLine($"Applying Simpson's 1/3 Rule with n = {n}, h = (b - a) / n = ({b} - {a}) / {n} = {h:F4}");
            steps.AppendLine($"\n────୨ৎ──── Step 1: Evaluate f(x) at the endpoints and intermediate points: ────୨ৎ────");
            double sum = 0;

            for (int i = 0; i <= n; i++)
            {
                double x = a + i * h;
                double y = EvaluateFunction(expression, x);
                if (double.IsNaN(y) || double.IsInfinity(y))
                {
                    throw new ArgumentException($"Function evaluation resulted in a non-finite value at x = {x}. Please check your function and bounds.");
                }
                steps.AppendLine($"  f(x_{i}) = f({x:F4}) = {y:F4}");
                if (i == 0 || i == n)
                    sum += y;
                else if (i % 2 == 0)
                    sum += 2 * y;
                else
                    sum += 4 * y;
            }

            steps.AppendLine($"\n────୨ৎ──── Step 2: Apply the Simpson's 1/3 Rule formula: ────୨ৎ────");
            steps.AppendLine($"🌷 Integral ≈ (h / 3) * [f(x_0) + 4f(x_1) + 2f(x_2) + 4f(x_3) + ... + 4f(x_{n - 1}) + f(x_n)]");
            steps.AppendLine($"            ≈ ({h:F4} / 3) * [{sum:F4}]");
            double result = (h / 3) * sum;
            steps.AppendLine($"            ≈ {result:F4}");

            return steps.ToString();
        }

        private double EvaluateFunction(string expression, double x)
        {
            var e = new Expression(expression);

            // Set 'x' parameter if it's actually used in the expression
            // This prevents NCalc from throwing "Parameter 'x' not found" errors if 'x' isn't in the expression.
            if (expression.Contains("x"))
            {
                e.Parameters["x"] = x;
            }

            e.EvaluateFunction += (name, args) =>
            {
                try
                {
                    // Handle 'pi' as a function for consistency, though 'e' is handled as a parameter.
                    if (name.ToLower() == "pi")
                    {
                        args.Result = Math.PI;
                        return;
                    }

                    // Only evaluate parameters for standard functions (e.g., sin(a), pow(a,b))
                    double a = args.Parameters.Count() > 0 ? Convert.ToDouble(args.Parameters[0].Evaluate()) : double.NaN;
                    double b = args.Parameters.Count() > 1 ? Convert.ToDouble(args.Parameters[1].Evaluate()) : double.NaN;

                    args.Result = name.ToLower() switch
                    {
                        "pow" => (a == 0 && b == 0) ? double.NaN : Math.Pow(a, b),
                        "ln" => a <= 0 ? double.NaN : Math.Log(a),
                        "log" => a <= 0 ? double.NaN : Math.Log10(a),
                        "sin" => Math.Sin(a),
                        "cos" => Math.Cos(a),
                        "tan" => Math.Tan(a),
                        "exp" => a > 700 ? double.PositiveInfinity : // Prevent overflow
                                   a < -700 ? 0 : Math.Exp(a),    // Prevent underflow
                        _ => double.NaN // Unknown function
                    };
                }
                catch
                {
                    args.Result = double.NaN; // Catch any evaluation errors within the function
                }
            };

            e.EvaluateParameter += (name, args) =>
            {
                // NCalc by default recognizes "PI" and "E" as parameters.
                // This makes sure 'e' and 'pi' are handled even if not explicitly as functions.
                if (name.ToLower() == "e")
                    args.Result = Math.E;
                else if (name.ToLower() == "pi")
                    args.Result = Math.PI;
            };

            try
            {
                // Attempt to evaluate the expression. If it's a simple number or constant, NCalc will handle it.
                object result = e.Evaluate();
                if (result is double d)
                {
                    return d;
                }
                // If the result is an int or long, convert it to double
                else if (result is int || result is long)
                {
                    return Convert.ToDouble(result);
                }
                else
                {
                    // If the result is not a numeric type (e.g., boolean from an invalid expression), return NaN
                    return double.NaN;
                }
            }
            catch (NCalc.EvaluationException)
            {
                // NCalc can throw EvaluationException for syntax errors or undefined parameters/functions.
                return double.NaN;
            }
            catch (Exception)
            {
                // Catch any other unexpected exceptions
                return double.NaN;
            }
        }

        private async Task TypeOutSolution(string output)
        {
            TextBlock_Solution.Inlines.Clear();

            foreach (string line in output.Split('\n'))
            {
                TextBlock_Solution.Inlines.Add(new Run { Text = line });
                TextBlock_Solution.Inlines.Add(new LineBreak());
                await Task.Delay(100);
            }
        }
        private void Button_Back_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }
}
