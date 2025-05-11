using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

        private void Button_Solve_Click(object sender, RoutedEventArgs e)
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

                string result = SolveUsingLinearRegression(xValues, yValues);
                TextBlock_Solution.Text = result;
            }
            else
            {
                TextBlock_Solution.Text = "Please select 'Linear Regression' to use this function.";
            }
        }

        private string SolveUsingLinearRegression(List<double> xValues, List<double> yValues)
        {
            int n = xValues.Count;

            if (n < 2)
                return "At least 2 data points are required.";

            double sumX = 0, sumY = 0, sumXY = 0, sumX2 = 0;

            for (int i = 0; i < n; i++)
            {
                sumX += xValues[i];
                sumY += yValues[i];
                sumXY += xValues[i] * yValues[i];
                sumX2 += xValues[i] * xValues[i];
            }

            double denominator = n * sumX2 - sumX * sumX;
            if (Math.Abs(denominator) < 1e-10)
                return "Cannot compute linear regression: denominator is zero.";

            double a = (n * sumXY - sumX * sumY) / denominator;
            double b = (sumY * sumX2 - sumX * sumXY) / denominator;

            return $"y = {a:F4}x + {b:F4}";
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
