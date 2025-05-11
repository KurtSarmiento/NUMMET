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

            // Show PointInputPanel only for Linear Regression (for now)
            if (selectedMethod == "Linear Regression")
            {
                ComboBox_PointCount.Visibility = Visibility.Visible;
                PointInputPanel.Visibility = Visibility.Visible;
            }
            else
            {
                PointInputPanel.Visibility = Visibility.Collapsed;
                ComboBox_PointCount.Visibility = Visibility.Collapsed;

                // Other logic depending on method (optional)
            }

            // Clear previous entries
            PointInputPanel.Children.Clear();
            TextBlock_Solution.Text = "";
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
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(5)
                    };

                    // Label
                    TextBlock label = new TextBlock
                    {
                        Text = $"Point {i + 1}:",
                        Width = 70,
                        VerticalAlignment = VerticalAlignment.Center
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
                    pointRow.Children.Add(new TextBlock { Text = "x:", VerticalAlignment = VerticalAlignment.Center });
                    pointRow.Children.Add(xInput);
                    pointRow.Children.Add(new TextBlock { Text = " y:", VerticalAlignment = VerticalAlignment.Center });
                    pointRow.Children.Add(yInput);

                    PointInputPanel.Children.Add(pointRow);
                }
            }
        }

        private async void Button_Solve_Click(object sender, RoutedEventArgs e)
        {
            if ((ComboBox_Method.SelectedItem as ComboBoxItem)?.Content.ToString() == "Linear Regression")
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
                        return;
                    }
                }

                string result = SolveLinearRegression(xValues, yValues);
                await TypeOutSolution(result);
                TextBlock_Solution.Text = result;
            }
            else
            {
                TextBlock_Solution.Text = "Please select 'Linear Regression' to use this function.";
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


        private void Button_Clear_Click(object sender, RoutedEventArgs e)
        {
            PointInputPanel.Children.Clear();
            TextBlock_Solution.Text = "";

            ComboBox_Method.SelectedIndex = -1;
            ComboBox_PointCount.SelectedIndex = -1;
        }

        private void Button_Back_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }
}
