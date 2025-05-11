using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using ScottPlot.WinUI;
using static ScottPlot.Drawing;
using ScottPlot.Colormaps;
using ScottPlot;
using System.Numerics;
using NCalc;

namespace NUMMET
{
    public sealed partial class CurveFittingPage : Page
    {
        public CurveFittingPage()
        {
            this.InitializeComponent();
        }

        private void ComboBox_Method_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedMethod = (ComboBox_Method.SelectedItem as ComboBoxItem)?.Content.ToString();

            // Show PointInputPanel for methods needing point input
            if (selectedMethod == "Linear Regression" || selectedMethod == "Polynomial Regression")
            {
                ComboBox_PointCount.Visibility = Visibility.Visible;
                PointInputPanel.Visibility = Visibility.Visible;
            }
            else
            {
                PointInputPanel.Visibility = Visibility.Collapsed;
                ComboBox_PointCount.Visibility = Visibility.Collapsed;
            }

            // Clear previous entries
            PointInputPanel.Children.Clear();
            TextBlock_Solution.Text = "";
            PlotView.Visibility = Visibility.Collapsed;
            PlotView.Plot.Clear();
            PlotView.Refresh();

            // Handle method-specific actions
            if (selectedMethod == "Polynomial Regression")
            {
                TextBlock_Solution.Text = "You have selected Polynomial Curve Fitting. Please input your data points and click 'Submit'.";
                // If you added a degree input, you might make it visible here.
            }
        }

        private void ComboBox_PointCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Clear previous input fields
            PointInputPanel.Children.Clear();

            // Get selected point count
            if ((ComboBox_PointCount.SelectedItem as ComboBoxItem)?.Content is string selected &&
                int.TryParse(selected, out int pointCount))
            {
                for (int i = 0; i < pointCount; i++)
                {
                    // Create a horizontal stack panel for each point
                    StackPanel pointRow = new StackPanel
                    {
                        Orientation = Microsoft.UI.Xaml.Controls.Orientation.Horizontal, // Fully qualified
                        Margin = new Thickness(5)
                    };

                    // Label
                    TextBlock label = new TextBlock
                    {
                        Text = $"Point {i + 1}:",
                        Width = 70,
                        VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center // Fully qualified
                    };

                    // X input
                    TextBox xInput = new TextBox
                    {
                        Width = 90,
                        Margin = new Thickness(5),
                        Tag = $"x{i}"
                    };

                    // Y input
                    TextBox yInput = new TextBox
                    {
                        Width = 90,
                        Margin = new Thickness(5),
                        Tag = $"y{i}"
                    };

                    pointRow.Children.Add(label);
                    pointRow.Children.Add(new TextBlock { Text = "x:", VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center }); // Fully qualified
                    pointRow.Children.Add(xInput);
                    pointRow.Children.Add(new TextBlock { Text = " y:", VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center }); // Fully qualified
                    pointRow.Children.Add(yInput);

                    PointInputPanel.Children.Add(pointRow);
                }
            }
            PlotView.Visibility = Visibility.Collapsed; // Hide when method changes
            PlotView.Plot.Clear();
            PlotView.Refresh();
        }

        private async void Button_Solve_Click(object sender, RoutedEventArgs e)
        {
            var selectedMethod = (ComboBox_Method.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (selectedMethod == "Linear Regression" || selectedMethod == "Least Squares")
            {
                // Existing logic for linear regression and least squares
                List<double> xValues = new List<double>();
                List<double> yValues = new List<double>();

                foreach (StackPanel row in PointInputPanel.Children)
                {
                    var inputs = row.Children.OfType<TextBox>().ToList();
                    if (inputs.Count >= 2 &&
                        double.TryParse(inputs[0].Text, out double x) &&
                        double.TryParse(inputs[1].Text, out double y))
                    {
                        xValues.Add(x);
                        yValues.Add(y);
                    }
                    else
                    {
                        TextBlock_Solution.Text = "Please ensure all x and y inputs are valid numbers.";
                        PlotView.Visibility = Visibility.Collapsed;
                        PlotView.Plot.Clear();
                        PlotView.Refresh();
                        return;
                    }
                }

                string result = SolveLinearRegression(xValues, yValues);
                await TypeOutSolution(result);
                TextBlock_Solution.Text = result;
                PlotLinearRegression(xValues, yValues);
                PlotView.Visibility = Visibility.Visible;
            }
            else if (selectedMethod == "Polynomial Regression")
            {
                List<double> xValues = new List<double>();
                List<double> yValues = new List<double>();

                foreach (StackPanel row in PointInputPanel.Children)
                {
                    var inputs = row.Children.OfType<TextBox>().ToList();
                    if (inputs.Count >= 2 &&
                        double.TryParse(inputs[0].Text, out double x) &&
                        double.TryParse(inputs[1].Text, out double y))
                    {
                        xValues.Add(x);
                        yValues.Add(y);
                    }
                    else
                    {
                        TextBlock_Solution.Text = "Please ensure all x and y inputs are valid numbers.";
                        PlotView.Visibility = Visibility.Collapsed;
                        PlotView.Plot.Clear();
                        PlotView.Refresh();
                        return;
                    }
                }

                // Assume a degree for the polynomial (e.g., 2 for quadratic)
                int polynomialDegree = 2;
                string result = SolvePolynomialRegression(xValues, yValues, polynomialDegree, out double[] coefficients);
                await TypeOutSolution(result);
                TextBlock_Solution.Text = result;
                PlotPolynomialRegression(xValues, yValues, coefficients);
                PlotView.Visibility = Visibility.Visible;
            }
            else
            {
                TextBlock_Solution.Text = "Please select a valid curve fitting method.";
                PlotView.Visibility = Visibility.Collapsed;
                PlotView.Plot.Clear();
                PlotView.Refresh();
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

        private string SolveLinearRegression(List<double> xValues, List<double> yValues)
        {
            int n = xValues.Count;

            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Step 1: Construct Table of Values");
            sb.AppendLine("x\t\ty\t\tx*y\t\tx^2");

            for (int i = 0; i < n; i++)
            {
                double x = xValues[i];
                double y = yValues[i];
                double xy = x * y;
                double x2 = x * x;

                sumX += x;
                sumY += y;
                sumXY += xy;
                sumX2 += x2;

                sb.AppendLine($"{x:F4}\t\t{y:F4}\t\t{xy:F4}\t\t{x2:F4}");
            }

            sb.AppendLine("\nStep 2: Calculate Summations");
            sb.AppendLine($"Σx = {sumX:F4}, Σy = {sumY:F4}, Σxy = {sumXY:F4}, Σx² = {sumX2:F4}");

            double m = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            double b = (sumY - m * sumX) / n;

            sb.AppendLine("\nStep 3: Compute the Slope (m) and Intercept (b)");
            sb.AppendLine($"m = (nΣxy - ΣxΣy) / (nΣx² - (Σx)²)");
            sb.AppendLine($"m = ({n} * {sumXY:F4} - {sumX:F4} * {sumY:F4}) / ({n} * {sumX2:F4} - {sumX:F4}²) = {m:F4}");
            sb.AppendLine($"b = (Σy - mΣx) / n = ({sumY:F4} - {m:F4} * {sumX:F4}) / {n} = {b:F4}");

            sb.AppendLine($"\n📈 Final Linear Regression Equation: y = {m:F4}x + {b:F4}");

            return sb.ToString();
        }

        private void PlotLinearRegression(List<double> xValues, List<double> yValues)
        {
            var plt = PlotView.Plot;
            plt.Clear();
            PlotView.Plot.FigureBackground.Color = Color.FromHex("#202020");
            PlotView.Plot.DataBackground.Color = Color.FromHex("#202020");
            PlotView.Plot.Axes.Color(Color.FromHex("#d7d7d7"));
            PlotView.Plot.Grid.MajorLineColor = Color.FromHex("#404040");
            PlotView.Plot.Legend.BackgroundColor = Color.FromHex("#404040");
            PlotView.Plot.Legend.FontColor = Color.FromHex("#fba2a1");
            PlotView.Plot.Legend.OutlineColor = Color.FromHex("#fba2a1");

            // Plot the points
            double[] xs = xValues.ToArray();
            double[] ys = yValues.ToArray();
            var scatterPoints = plt.Add.Scatter(xs, ys);
            scatterPoints.MarkerSize = 5;
            scatterPoints.Color = Color.FromHex("#808080");
            scatterPoints.Label = "Points"; // Set the label property

            // Calculate the regression line
            int n = xValues.Count;
            double sumX = xValues.Sum();
            double sumY = yValues.Sum();
            double sumXY = xValues.Zip(yValues, (x, y) => x * y).Sum();
            double sumX2 = xValues.Sum(x => x * x);

            double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            double intercept = (sumY - slope * sumX) / n;

            // Generate points for the regression line
            double[] lineXs = { xs.Min(), xs.Max() };
            double[] lineYs = lineXs.Select(x => slope * x + intercept).ToArray();

            // Plot the regression line
            var scatterLine = plt.Add.Scatter(lineXs, lineYs);
            scatterLine.LineWidth = 2;
            scatterLine.Label = "Regression Line"; // Set the label property
            scatterLine.Color = Color.FromHex("#fba2a1");

            // Enable and configure the legend
            plt.Legend.IsVisible = true; // Enable the legend
            plt.Legend.Location = ScottPlot.Alignment.UpperRight; // Use ScottPlot's Alignment enum

            // Refresh the plot
            PlotView.Refresh();
        }

        private string SolvePolynomialRegression(List<double> xValues, List<double> yValues, int degree, out double[] coefficients)
        {
            // Input validation:
            if (xValues == null || yValues == null || xValues.Count != yValues.Count || xValues.Count == 0)
            {
                coefficients = null;
                return "Error: Invalid input data. X and Y values must be non-empty and have the same number of points.";
            }

            if (degree < 1)
            {
                coefficients = null;
                return "Error: Polynomial degree must be 1 or greater.";
            }

            if (xValues.Count <= degree)
            {
                coefficients = null;
                return "Error: Number of data points must be greater than the polynomial degree to solve for coefficients.";
            }

            try
            {
                int n = xValues.Count;
                int m = degree + 1; // Number of coefficients

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Polynomial Regression (Degree {degree})");
                sb.AppendLine("\n================================================================");
                sb.AppendLine("Step 1: Construct the Vandermonde Matrix A");
                sb.AppendLine("================================================================");

                // 1. Construct the Vandermonde matrix A (as a 2D array).
                double[,] A = new double[n, m];
                sb.Append("A = \n");
                for (int i = 0; i < n; i++)
                {
                    sb.Append("[ ");
                    for (int j = 0; j < m; j++)
                    {
                        A[i, j] = Math.Pow(xValues[i], j);
                        sb.Append($"{A[i, j]:F4}, ");
                    }
                    sb.AppendLine("]");
                }

                // 2. Create the vector y (as a double array).
                double[] y = yValues.ToArray();
                sb.AppendLine("\n================================================================");
                sb.AppendLine("Step 2: Create the vector y");
                sb.AppendLine("================================================================");
                sb.Append("y = [ ");
                for (int i = 0; i < n; i++)
                {
                    sb.Append($"{y[i]:F4}, ");
                }
                sb.AppendLine("]");

                // 3. Calculate A^T * A  and A^T * y manually.
                double[,] AtA = new double[m, m];
                double[] Aty = new double[m];

                sb.AppendLine("\n================================================================");
                sb.AppendLine("Step 3: Calculate AᵀA (A Transpose times A)");
                sb.AppendLine("================================================================");
                // Calculate A^T * A
                for (int i = 0; i < m; i++)
                {
                    for (int j = 0; j < m; j++)
                    {
                        AtA[i, j] = 0;
                        for (int k = 0; k < n; k++)
                        {
                            AtA[i, j] += A[k, i] * A[k, j];
                        }
                        // sb.Append($"{AtA[i, j]:F4}\t");
                    }
                    // sb.AppendLine();
                }
                sb.Append("AᵀA = \n");
                for (int i = 0; i < m; i++)
                {
                    sb.Append("[ ");
                    for (int j = 0; j < m; j++)
                    {
                        sb.Append($"{AtA[i, j]:F4}, ");
                    }
                    sb.AppendLine("]");
                }

                sb.AppendLine("\n================================================================");
                sb.AppendLine("Step 4: Calculate Aᵀy (A Transpose times y)");
                sb.AppendLine("================================================================");
                // Calculate A^T * y
                for (int i = 0; i < m; i++)
                {
                    Aty[i] = 0;
                    for (int k = 0; k < n; k++)
                    {
                        Aty[i] += A[k, i] * y[k];
                    }
                    //sb.Append($"{Aty[i]:F4}\t");
                }
                //sb.AppendLine();
                sb.Append("Aᵀy = [ ");
                for (int i = 0; i < m; i++)
                {
                    sb.Append($"{Aty[i]:F4}, ");
                }
                sb.AppendLine("]");

                // 4. Solve the system of linear equations (AtA * c = Aty) using Gaussian elimination.
                coefficients = SolveLinearSystem(AtA, Aty);
                if (coefficients == null)
                {
                    return "Error: Unable to solve the system of equations.  Matrix is singular."; // Error from SolveLinearSystem
                }

                sb.AppendLine("\n================================================================");
                sb.AppendLine("Step 5: Solve for Coefficients using Gaussian Elimination");
                sb.AppendLine("================================================================");
                sb.AppendLine("Coefficients:");
                for (int i = 0; i <= degree; i++)
                {
                    sb.AppendLine($"c[{i}] = {coefficients[i]:F4}");
                }

                string equation = "y = ";
                for (int i = 0; i <= degree; i++)
                {
                    if (i == 0)
                    {
                        equation += $"{coefficients[i]:F4}";
                    }
                    else if (i == 1)
                    {
                        equation += $" + {coefficients[i]:F4}x";
                    }
                    else
                    {
                        equation += $" + {coefficients[i]:F4}x^{i}";
                    }
                }
                sb.AppendLine($"\n📈 Final Polynomial Regression Equation: {equation}");

                return sb.ToString();
            }
            catch (Exception ex)
            {
                coefficients = null;
                return "An error occurred during calculation: " + ex.Message;
            }
        }


        private double[] SolveLinearSystem(double[,] matrix, double[] rightSide)
        {
            int n = rightSide.Length;
            double[,] augmentedMatrix = new double[n, n + 1];

            // Create augmented matrix [AtA | Aty]
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    augmentedMatrix[i, j] = matrix[i, j];
                }
                augmentedMatrix[i, n] = rightSide[i];
            }

            // Perform Gaussian elimination with partial pivoting
            for (int p = 0; p < n; p++)
            {
                // Find pivot row with largest absolute value in current column
                int maxRow = p;
                for (int i = p + 1; i < n; i++)
                {
                    if (Math.Abs(augmentedMatrix[i, p]) > Math.Abs(augmentedMatrix[maxRow, p]))
                    {
                        maxRow = i;
                    }
                }

                // Swap current row with pivot row
                if (maxRow != p)
                {
                    for (int j = 0; j <= n; j++)
                    {
                        double temp = augmentedMatrix[p, j];
                        augmentedMatrix[p, j] = augmentedMatrix[maxRow, j];
                        augmentedMatrix[maxRow, j] = temp;
                    }
                }

                // Check if matrix is singular (pivot element is zero)
                if (Math.Abs(augmentedMatrix[p, p]) < 1e-10) // Use a small tolerance
                {
                    return null; // Matrix is singular; cannot solve
                }

                // Eliminate below pivot
                for (int i = p + 1; i < n; i++)
                {
                    double factor = augmentedMatrix[i, p] / augmentedMatrix[p, p];
                    for (int j = p + 1; j <= n; j++)
                    {
                        augmentedMatrix[i, j] -= factor * augmentedMatrix[p, j];
                    }
                }
            }

            // Back substitution
            double[] solution = new double[n];
            for (int i = n - 1; i >= 0; i--)
            {
                double sum = 0;
                for (int j = i + 1; j < n; j++)
                {
                    sum += augmentedMatrix[i, j] * solution[j];
                }
                solution[i] = (augmentedMatrix[i, n] - sum) / augmentedMatrix[i, i];
            }
            return solution;
        }

        private void PlotPolynomialRegression(List<double> xValues, List<double> yValues, double[] coefficients)
        {
            var plt = PlotView.Plot;
            plt.Clear();
            PlotView.Plot.FigureBackground.Color = Color.FromHex("#202020");
            PlotView.Plot.DataBackground.Color = Color.FromHex("#202020");
            PlotView.Plot.Axes.Color(Color.FromHex("#d7d7d7"));
            PlotView.Plot.Grid.MajorLineColor = Color.FromHex("#404040");
            PlotView.Plot.Legend.BackgroundColor = Color.FromHex("#404040");
            PlotView.Plot.Legend.FontColor = Color.FromHex("#fba2a1");
            PlotView.Plot.Legend.OutlineColor = Color.FromHex("#fba2a1");
            // Apply your desired plot styling (background, axes, etc.) here

            // Plot the original points
            double[] xs = xValues.ToArray();
            double[] ys = yValues.ToArray();
            var scatterPlot = plt.Add.Scatter(xs, ys); // Use AddScatter
            scatterPlot.Color = Color.FromHex("#808080");
            scatterPlot.MarkerSize = 5;
            scatterPlot.Label = "Data Points";

            // Generate points for the polynomial curve
            double minX = xs.Min();
            double maxX = xs.Max();
            double xStep = (maxX - minX) / 100; // Generate more points for a smooth curve
            List<double> curveXs = new List<double>();
            List<double> curveYs = new List<double>();

            for (double x = minX; x <= maxX; x += xStep)
            {
                curveXs.Add(x);
                double y = 0;
                for (int i = 0; i < coefficients.Length; i++)
                {
                    y += coefficients[i] * Math.Pow(x, i);
                }
                curveYs.Add(y);
            }

            // Plot the polynomial curve
            var linePlot = plt.Add.Scatter(curveXs.ToArray(), curveYs.ToArray()); // Use AddLine
            linePlot.LineWidth = 2;
            linePlot.Color = Color.FromHex("#fba2a1");
            linePlot.Color = ScottPlot.Color.FromHex("#f08080"); // Use ScottPlot.Color
            linePlot.Label = $"Polynomial (Degree {coefficients.Length - 1})";

            plt.Legend.IsVisible = true;
            plt.Legend.Location = ScottPlot.Alignment.UpperRight;

            PlotView.Refresh();
        }

        private void Button_Clear_Click(object sender, RoutedEventArgs e)
        {
            PointInputPanel.Children.Clear();
            TextBlock_Solution.Text = "";
            PlotView.Visibility = Visibility.Collapsed; // Hide on clear
            PlotView.Plot.Clear();
            PlotView.Refresh();

            ComboBox_Method.SelectedIndex = -1;
            ComboBox_PointCount.SelectedIndex = -1;
        }

        private void Button_Back_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }
}
