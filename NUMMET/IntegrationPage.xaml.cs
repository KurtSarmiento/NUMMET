﻿using System;
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
using ScottPlot;
using ScottPlot.WinUI;
using Windows.Media.Playback;
using Windows.Media.Core;

namespace NUMMET
{
    public sealed partial class IntegrationPage : Page
    {
        public IntegrationPage()
        {
            this.InitializeComponent();
            Button_Solve.Click += Button_Solve_Click;
            Button_Clear.Click += Button_Clear_Click;
            plotView.Visibility = Visibility.Collapsed;
        }

        private void ComboBox_Method_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ExpressionInput.Text = "";
            IntegrationLowerBound.Text = "";
            IntegrationUpperBound.Text = "";
            Division_Number.Text = "";
            TextBlock_Solution.Text = "";
            plotView.Visibility = Visibility.Collapsed;
            plotView.Plot.Clear();
            plotView.Refresh();
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
                    var player = new MediaPlayer();
                    player.Source = MediaSource.CreateFromUri(new Uri("ms-appx:///submit.mp3"));
                    player.Play();
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
                        (string solutionSteps, double[] x_points, double[] y_points) = TrapezoidalRuleWithPlotData(expression, a, b, n);
                        await TypeOutSolution(solutionSteps);
                        TextBlock_Solution.Text = solutionSteps;
                        PlotTrapezoidal(expression, a, b, n, x_points, y_points);
                        plotView.Visibility = Visibility.Visible; // Show the plot view
                    }
                    else if (method == "Simpson's 1/3")
                    {
                        if (!int.TryParse(Division_Number.Text, out int n) || n <= 0 || n % 2 != 0)
                        {
                            TextBlock_Solution.Text = "Error: Invalid number of divisions for Simpson's 1/3 Rule. Must be a positive even integer.";
                            return;
                        }
                        (string solutionSteps, double[] x_points, double[] y_points) = SimpsonsRuleWithPlotData(expression, a, b, n);
                        await TypeOutSolution(solutionSteps);
                        TextBlock_Solution.Text = solutionSteps;
                        PlotSimpsons(expression, a, b, n, x_points, y_points);
                        plotView.Visibility = Visibility.Visible; // Show the plot view
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
            Division_Number.Text = "";
            plotView.Plot.Clear(); // Clear the plot instead of assigning a new value
            plotView.Plot.RenderInMemory(); // Refresh the plot to reflect the changes
            plotView.Visibility = Visibility.Collapsed; // Hide the plot view
        }

        private (string steps, double[] x_points, double[] y_points) TrapezoidalRuleWithPlotData(string expression, double a, double b, int n)
        {
            StringBuilder steps = new StringBuilder();
            double h = (b - a) / n;
            steps.AppendLine($"Applying Trapezoidal Rule with n = {n}, h = ({b} - {a}) / {n} = {h:F4}");
            steps.AppendLine($"\n──── Step 1: Evaluate f(x) at the endpoints and intermediate points: ────");
            double sum = 0;
            double[] x_values = new double[n + 1];
            double[] y_values = new double[n + 1];

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
                x_values[i] = x;
                y_values[i] = y;
            }

            steps.AppendLine($"\n──── Step 2: Apply the Trapezoidal Rule formula: ────");
            steps.AppendLine($"Integral ≈ (h / 2) * [f(x_0) + 2f(x_1) + ... + 2f(x_{n - 1}) + f(x_n)]");
            steps.AppendLine($"         ≈ ({h:F4} / 2) * [{sum:F4}]");
            double result = (h / 2) * sum;
            steps.AppendLine($"         ≈ {result:F4}");

            return (steps.ToString(), x_values, y_values);
        }

        private (string steps, double[] x_points, double[] y_points) SimpsonsRuleWithPlotData(string expression, double a, double b, int n)
        {
            StringBuilder steps = new StringBuilder();
            if (n % 2 != 0)
            {
                throw new ArgumentException("The number of divisions for Simpson's 1/3 Rule must be an even integer.");
            }

            double h = (b - a) / n;
            steps.AppendLine($"Applying Simpson's 1/3 Rule with n = {n}, h = ({b} - {a}) / {n} = {h:F4}");
            steps.AppendLine($"\n──── Step 1: Evaluate f(x) at the endpoints and intermediate points: ────");
            double sum = 0;
            double[] x_values = new double[n + 1];
            double[] y_values = new double[n + 1];

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
                x_values[i] = x;
                y_values[i] = y;
            }

            steps.AppendLine($"\n──── Step 2: Apply the Simpson's 1/3 Rule formula: ────");
            steps.AppendLine($"Integral ≈ (h / 3) * [f(x_0) + 4f(x_1) + 2f(x_2) + 4f(x_3) + ... + 4f(x_{n - 1}) + f(x_n)]");
            steps.AppendLine($"         ≈ ({h:F4} / 3) * [{sum:F4}]");
            double result = (h / 3) * sum;
            steps.AppendLine($"         ≈ {result:F4}");

            return (steps.ToString(), x_values, y_values);
        }

        private void PlotTrapezoidal(string expression, double a, double b, int n, double[] x_points, double[] y_points)
        {
            var plot = plotView.Plot; // Access the existing Plot instance

            // Clear the existing plot to avoid overlapping
            plot.Clear();
            plotView.Plot.FigureBackground.Color = Color.FromHex("#202020");
            plotView.Plot.DataBackground.Color = Color.FromHex("#202020");
            plotView.Plot.Axes.Color(Color.FromHex("#d7d7d7"));
            plotView.Plot.Grid.MajorLineColor = Color.FromHex("#404040");

            // Plot the original function
            double[] x_fine = Linspace(a, b, 200);
            double[] y_fine = x_fine.Select(x => EvaluateFunction(expression, x)).ToArray();
            plot.Add.Scatter(x_fine, y_fine, ScottPlot.Color.FromHex("#fba2a1"));

            // Plot the trapezoids
            for (int i = 0; i < n; i++)
            {
                double[] x_trap = { x_points[i], x_points[i + 1], x_points[i + 1], x_points[i] };
                double[] y_trap = { 0, 0, y_points[i + 1], y_points[i] };
                var polygon = plot.Add.Polygon(x_trap, y_trap);
                polygon.FillColor = ScottPlot.Color.FromARGB(0x99fba2a1);
                polygon.LineColor = ScottPlot.Color.FromHex("#fba2a1"); // Corrected line

                plot.Add.Line(x_points[i], y_points[i], x_points[i + 1], y_points[i + 1]).Color = ScottPlot.Color.FromHex("#000000");
            }

            plot.Title($"Trapezoidal Rule (n={n})");
            plot.XLabel("x");
            plot.YLabel("f(x)");
            plot.ShowLegend();

            plotView.Refresh(); // Refresh the plot to reflect the changes
        }

        private void PlotSimpsons(string expression, double a, double b, int n, double[] x_points, double[] y_points)
        {
            var plot = plotView.Plot; // Access the existing Plot instance

            // Clear the existing plot to avoid overlapping
            plot.Clear();

            // Plot the original function
            double[] x_fine = Linspace(a, b, 200);
            double[] y_fine = x_fine.Select(x => EvaluateFunction(expression, x)).ToArray();
            plot.Add.Scatter(x_fine, y_fine, ScottPlot.Color.FromHex("#fba2a1"));

            // Visualize the points used for Simpson's rule
            plot.Add.ScatterPoints(x_points, y_points, color: ScottPlot.Color.FromARGB(0xFF404040)); // Blue color
            plot.Title($"Simpson's 1/3 Rule (n={n})");
            plot.XLabel("x");
            plot.YLabel("f(x)");
            plot.ShowLegend(); // Corrected this line

            plotView.Refresh();

            // Note: Visualizing the parabolic segments of Simpson's rule directly is more complex.
            // This visualization focuses on the points used.
        }

        private double[] Linspace(double start, double end, int numPoints)
        {
            double[] result = new double[numPoints];
            for (int i = 0; i < numPoints; i++)
            {
                result[i] = start + (end - start) * i / (numPoints - 1.0);
            }
            return result;
        }

        private double EvaluateFunction(string expression, double x)
        {
            var e = new Expression(expression);

            // Set 'x' parameter if it's actually used in the expression
            if (expression.Contains("x"))
            {
                e.Parameters["x"] = x;
            }

            e.EvaluateFunction += (name, args) =>
            {
                try
                {
                    if (name.ToLower() == "pi")
                    {
                        args.Result = Math.PI;
                        return;
                    }

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
                        "cot" => (Math.Abs(Math.Tan(a)) < 1e-9) ? double.NaN : 1.0 / Math.Tan(a),
                        "csc" => (Math.Abs(Math.Sin(a)) < 1e-9) ? double.NaN : 1.0 / Math.Sin(a),
                        "sec" => (Math.Abs(Math.Cos(a)) < 1e-9) ? double.NaN : 1.0 / Math.Cos(a),
                        "asin" => (a < -1 || a > 1) ? double.NaN : Math.Asin(a),
                        "acos" => (a < -1 || a > 1) ? double.NaN : Math.Acos(a),
                        "atan" => Math.Atan(a),
                        "atan2" => Math.Atan2(a, b), // Note the order of arguments here
                        "sinh" => Math.Sinh(a),
                        "cosh" => Math.Cosh(a),
                        "tanh" => Math.Tanh(a),
                        "exp" => a > 700 ? double.PositiveInfinity :
                                 a < -700 ? 0 : Math.Exp(a),
                        "sqrt" => a < 0 ? double.NaN : Math.Sqrt(a),
                        "abs" => Math.Abs(a),
                        "ceiling" => Math.Ceiling(a),
                        "floor" => Math.Floor(a),
                        "round" => Math.Round(a), // Can have an optional second argument for digits
                        "sign" => Math.Sign(a),
                        "log2" => a <= 0 ? double.NaN : Math.Log2(a),
                        "log10" => a <= 0 ? double.NaN : Math.Log10(a), // Redundant, already included as "log"
                        "expm1" => Math.Exp(a) - 1,
                        "log1p" => a <= -1 ? double.NaN : Math.Log(1 + a),
                        _ => double.NaN // Unknown function
                    };

                    // Handle optional second argument for Round
                    if (name.ToLower() == "round" && args.Parameters.Count() > 1)
                    {
                        int digits = Convert.ToInt32(args.Parameters[1].Evaluate());
                        args.Result = Math.Round(a, digits);
                    }
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
                else if (name.ToLower() == "pi")
                    args.Result = Math.PI;
            };

            try
            {
                object result = e.Evaluate();
                if (result is double d)
                {
                    return d;
                }
                else if (result is int || result is long)
                {
                    return Convert.ToDouble(result);
                }
                else
                {
                    return double.NaN;
                }
            }
            catch (NCalc.EvaluationException)
            {
                return double.NaN;
            }
            catch (Exception)
            {
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
