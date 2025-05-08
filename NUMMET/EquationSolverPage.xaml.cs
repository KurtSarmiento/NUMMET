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
using System.Text;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace NUMMET
{
    public sealed partial class EquationSolverPage : Page
    {
        private int equationCount = 0;
        private List<List<TextBox>> coefficientTextBoxes = new List<List<TextBox>>();
        private List<TextBox> rhsTextBoxes = new List<TextBox>();
        private readonly string[] variableNames = { "x", "y", "z", "w", "v" };
        public EquationSolverPage()
        {
            this.InitializeComponent();
        }
        private void ComboBox_EquationCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EquationInputPanel.Children.Clear();
            coefficientTextBoxes.Clear();
            rhsTextBoxes.Clear();

            if (ComboBox_EquationCount.SelectedItem is ComboBoxItem selectedItem &&
                int.TryParse(selectedItem.Content.ToString(), out equationCount))
            {
                for (int i = 0; i < equationCount; i++)
                {
                    StackPanel row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
                    List<TextBox> rowTextBoxes = new List<TextBox>();

                    for (int j = 0; j < equationCount; j++)
                    {
                        TextBox coeffBox = new TextBox
                        {
                            Width = 50,
                            Margin = new Thickness(5),
                            PlaceholderText = "0"
                        };
                        rowTextBoxes.Add(coeffBox);
                        row.Children.Add(coeffBox);

                        row.Children.Add(new TextBlock
                        {
                            Text = " " + variableNames[j] + (j < equationCount - 1 ? " + " : " "),
                            VerticalAlignment = VerticalAlignment.Center
                        });
                    }

                    row.Children.Add(new TextBlock
                    {
                        Text = "= ",
                        VerticalAlignment = VerticalAlignment.Center
                    });

                    TextBox rhsBox = new TextBox
                    {
                        Width = 50,
                        Margin = new Thickness(5),
                        PlaceholderText = "0"
                    };
                    rhsTextBoxes.Add(rhsBox);
                    row.Children.Add(rhsBox);

                    coefficientTextBoxes.Add(rowTextBoxes);
                    EquationInputPanel.Children.Add(row);
                }
            }
        }
        private void Button_Solve_Click(object sender, RoutedEventArgs e)
        {
            double[,] matrix = new double[equationCount, equationCount + 1];

            try
            {
                for (int i = 0; i < equationCount; i++)
                {
                    for (int j = 0; j < equationCount; j++)
                    {
                        matrix[i, j] = double.Parse(coefficientTextBoxes[i][j].Text);
                    }
                    matrix[i, equationCount] = double.Parse(rhsTextBoxes[i].Text);
                }

                var result = SolveUsingGaussElimination(matrix, equationCount);
                TextBlock_Solution.Text = result;
            }
            catch (Exception ex)
            {
                TextBlock_Solution.Text = "Error: " + ex.Message;
            }
        }

        private void Button_Clear_Click(object sender, RoutedEventArgs e)
        {
            foreach (var row in coefficientTextBoxes)
                foreach (var tb in row)
                    tb.Text = "";

            foreach (var tb in rhsTextBoxes)
                tb.Text = "";

            TextBlock_Solution.Text = "";
        }
        private string SolveUsingGaussElimination(double[,] augmentedMatrix, int n)
        {
            StringBuilder steps = new StringBuilder();

            // 1. Initial Augmented Matrix
            steps.AppendLine("🔹 Initial Augmented Matrix:");
            steps.AppendLine(MatrixToString(augmentedMatrix, n));

            // 2. Forward Elimination with Steps
            for (int i = 0; i < n; i++)
            {
                if (Math.Abs(augmentedMatrix[i, i]) < 1e-10)
                    return $"Math Error: Division by zero or near-zero pivot at row {i + 1}.";

                for (int j = i + 1; j < n; j++)
                {
                    double ratio = augmentedMatrix[j, i] / augmentedMatrix[i, i];

                    steps.AppendLine($"🔁 R{j + 1} = R{j + 1} - ({ratio:F3}) * R{i + 1}");

                    for (int k = 0; k <= n; k++)
                    {
                        augmentedMatrix[j, k] -= ratio * augmentedMatrix[i, k];
                    }
                }

                steps.AppendLine("🔹 Matrix after elimination step:");
                steps.AppendLine(MatrixToString(augmentedMatrix, n));
            }

            // Normalize diagonal elements to 1
            steps.AppendLine("🔁 Normalizing Diagonal to 1:");
            for (int i = 0; i < n; i++)
            {
                double diag = augmentedMatrix[i, i];
                if (Math.Abs(diag) < 1e-10)
                    return $"Math Error: Cannot normalize row {i + 1} (zero pivot).";

                steps.AppendLine($"R{i + 1} = R{i + 1} / {diag:F3}");

                for (int j = 0; j <= n; j++)
                {
                    augmentedMatrix[i, j] /= diag;
                }
            }
            steps.AppendLine("🔹 Matrix after normalizing diagonal:");
            steps.AppendLine(MatrixToString(augmentedMatrix, n));


            // 3. Back Substitution
            double[] x = new double[n];
            steps.AppendLine("🔙 Back Substitution:");
            for (int i = n - 1; i >= 0; i--)
            {
                x[i] = augmentedMatrix[i, n];
                for (int j = i + 1; j < n; j++)
                {
                    x[i] -= augmentedMatrix[i, j] * x[j];
                }
                x[i] /= augmentedMatrix[i, i];

                steps.AppendLine($"{variableNames[i]} = {x[i]:F6}");
            }

            return steps.ToString();
        }
        private string MatrixToString(double[,] matrix, int n)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < n; i++)
            {
                sb.Append("| ");
                for (int j = 0; j < n; j++)
                {
                    sb.Append($"{matrix[i, j],8:F3} ");
                }
                sb.Append("| ");
                sb.Append($"{matrix[i, n],8:F3} |\n");
            }
            return sb.ToString();
        }


        private void ComboBox_Method_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TextBlock_Solution.Text = ""; 
        }

        private void Button_Back_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }
}