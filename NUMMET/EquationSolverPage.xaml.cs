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
using Microsoft.UI.Xaml.Documents;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.Media.Core;

namespace NUMMET
{
    public sealed partial class EquationSolverPage : Page
    {
        private int equationCount = 0;
        private List<List<TextBox>> coefficientTextBoxes = new List<List<TextBox>>();
        private List<TextBox> rhsTextBoxes = new List<TextBox>();
        private List<TextBox> initialGuessTextBoxes = new List<TextBox>();
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
            UpdateGuessVisibility();
        }

        private void UpdateGuessVisibility()
        {
            var selectedMethod = (ComboBox_Method.SelectedItem as ComboBoxItem)?.Content.ToString();
            var selectedCount = (ComboBox_EquationCount.SelectedItem as ComboBoxItem)?.Content.ToString();

            if (selectedMethod == "Gauss-Seidel" && int.TryParse(selectedCount, out int count))
            {
                InitialGuessPanel.Visibility = Visibility.Visible;
                GenerateGuessInputs(count);
            }
            else
            {
                InitialGuessPanel.Visibility = Visibility.Collapsed;
                GuessInputFields.Children.Clear();
                initialGuessTextBoxes.Clear();
            }
        }

        private void GenerateGuessInputs(int count)
        {
            GuessInputFields.Children.Clear();
            initialGuessTextBoxes.Clear();
            string[] variableNames = { "x", "y", "z", "w", "v" };

            for (int i = 0; i < count; i++)
            {
                var tb = new TextBox
                {
                    Width = 50,
                    Height = 40,
                    Margin = new Thickness(5),
                    PlaceholderText = $"{variableNames[i]}"
                };
                GuessInputFields.Children.Add(tb);
                initialGuessTextBoxes.Add(tb);
            }
        }

        private async void Button_Solve_Click(object sender, RoutedEventArgs e)
        {
            var player = new MediaPlayer();
            player.Source = MediaSource.CreateFromUri(new Uri("ms-appx:///submit.mp3"));
            player.Play();

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

                string result = "";

                if (ComboBox_Method.SelectedItem is ComboBoxItem selectedMethod)
                {
                    switch (selectedMethod.Content.ToString())
                    {
                        case "Gauss-Jordan":
                            result = SolveUsingGaussJordan(matrix, equationCount);
                            break;
                        case "Gauss-Seidel":
                            double[] initialGuesses = new double[equationCount];
                            for (int i = 0; i < equationCount; i++)
                            {
                                if (double.TryParse(initialGuessTextBoxes[i].Text, out double guess))
                                {
                                    initialGuesses[i] = guess;
                                }
                                else
                                {
                                    TextBlock_Solution.Text = "Error: Please enter valid initial guesses for all variables.";
                                    return;
                                }
                            }
                            result = SolveUsingGaussSeidel(matrix, equationCount, initialGuesses: initialGuesses);
                            break;
                        default:
                            result = SolveUsingGaussElimination(matrix, equationCount);
                            break;
                    }
                }

                await TypeOutSolution(result);
            }
            catch (Exception ex)
            {
                TextBlock_Solution.Text = "Error: " + ex.Message;
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

        private void Button_Clear_Click(object sender, RoutedEventArgs e)
        {
            foreach (var row in coefficientTextBoxes)
                foreach (var tb in row)
                    tb.Text = "";

            foreach (var tb in rhsTextBoxes)
                tb.Text = "";

            foreach (var tb in initialGuessTextBoxes)
                tb.Text = "";

            TextBlock_Solution.Text = "";
        }

        private string SolveUsingGaussElimination(double[,] augmentedMatrix, int n)
        {
            StringBuilder steps = new StringBuilder();

            steps.AppendLine("────୨ৎ──── Initial Augmented Matrix: ────୨ৎ────");
            steps.AppendLine(MatrixToString(augmentedMatrix, n));

            for (int i = 0; i < n; i++)
            {
                if (Math.Abs(augmentedMatrix[i, i]) < 1e-10)
                    return $"Math Error: Division by zero or near-zero pivot at row {i + 1}.";

                for (int j = i + 1; j < n; j++)
                {
                    double ratio = augmentedMatrix[j, i] / augmentedMatrix[i, i];

                    steps.AppendLine($"🌷 R{j + 1} = R{j + 1} - ({ratio:F3}) * R{i + 1}");

                    for (int k = 0; k <= n; k++)
                    {
                        augmentedMatrix[j, k] -= ratio * augmentedMatrix[i, k];
                    }
                }

                steps.AppendLine("────୨ৎ──── Matrix after elimination step: ────୨ৎ────");
                steps.AppendLine(MatrixToString(augmentedMatrix, n));
            }

            steps.AppendLine("🌷 Normalizing Diagonal to 1:");
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
            steps.AppendLine("────୨ৎ──── Matrix after normalizing diagonal: ────୨ৎ────");
            steps.AppendLine(MatrixToString(augmentedMatrix, n));

            double[] x = new double[n];
            steps.AppendLine("🌸 Back Substitution:");
            for (int i = n - 1; i >= 0; i--)
            {
                x[i] = augmentedMatrix[i, n];
                for (int j = i + 1; j < n; j++)
                {
                    x[i] -= augmentedMatrix[i, j] * x[j];
                }
                x[i] /= augmentedMatrix[i, i];

                steps.AppendLine($"{variableNames[i]} = {x[i]:F4}");
            }

            return steps.ToString();
        }

        private string SolveUsingGaussJordan(double[,] augmentedMatrix, int n)
        {
            StringBuilder steps = new StringBuilder();

            steps.AppendLine("────୨ৎ──── Initial Augmented Matrix: ────୨ৎ────");
            steps.AppendLine(MatrixToString(augmentedMatrix, n));

            for (int i = 0; i < n; i++)
            {
                double pivot = augmentedMatrix[i, i];
                if (Math.Abs(pivot) < 1e-10)
                    return $"Math Error: Zero pivot found at row {i + 1}.";

                steps.AppendLine($"🌷 R{i + 1} = R{i + 1} / {pivot:F3}");
                for (int j = 0; j <= n; j++)
                {
                    augmentedMatrix[i, j] /= pivot;
                }

                for (int k = 0; k < n; k++)
                {
                    if (k == i) continue;

                    double factor = augmentedMatrix[k, i];
                    steps.AppendLine($"🌷 R{k + 1} = R{k + 1} - ({factor:F3}) * R{i + 1}");

                    for (int j = 0; j <= n; j++)
                    {
                        augmentedMatrix[k, j] -= factor * augmentedMatrix[i, j];
                    }
                }

                steps.AppendLine("────୨ৎ──── Matrix after step: ────୨ৎ────");
                steps.AppendLine(MatrixToString(augmentedMatrix, n));
            }

            steps.AppendLine("🌸 Final Solution:");
            for (int i = 0; i < n; i++)
            {
                steps.AppendLine($"{variableNames[i]} = {augmentedMatrix[i, n]:F4}");
            }

            return steps.ToString();
        }

        private string SolveUsingGaussSeidel(double[,] augmentedMatrix, int n, int maxIterations = 100, double tolerance = 1e-6, double[] initialGuesses = null)
        {
            StringBuilder steps = new StringBuilder();
            double[] x = initialGuesses ?? new double[n];
            double[] prevX = new double[n];
            int j = 0;

            steps.AppendLine("────୨ৎ──── Initial Guesses: ────୨ৎ────");
            for (int i = 0; i < n; i++)
            {
                steps.AppendLine($"{variableNames[i]} = {x[i]:F4}");
            }
            steps.AppendLine("\nFor each variable, the Gauss-Seidel formula is applied:");
            for (int i = 0; i < n; i++)
            {
                steps.AppendLine($"{variableNames[i]} = (b[{i}] - sum(a[{i},{j}] * x[{j}] for j ≠ {i})) / a[{i},{i}]");
            }
            steps.AppendLine("\n────୨ৎ──── Starting iterations ────୨ৎ────\n");

            for (int iter = 1; iter <= maxIterations; iter++)
            {
                steps.AppendLine($"🌷 Iteration {iter}:");

                for (int i = 0; i < n; i++)
                {
                    double sum = augmentedMatrix[i, n];
                    for (j = 0; j < n; j++)
                    {
                        if (i != j)
                        {
                            sum -= augmentedMatrix[i, j] * x[j];
                        }
                    }

                    double newX = sum / augmentedMatrix[i, i];

                    steps.AppendLine($"{variableNames[i]} = ({augmentedMatrix[i, n]} - " + string.Join(" - ", Enumerable.Range(0, n).Where(k => k != i).Select(k => $"{augmentedMatrix[i, k]} * {x[k]:F4}")) + $") / {augmentedMatrix[i, i]} = {newX:F5}");

                    prevX[i] = x[i];
                    x[i] = newX;
                }

                bool isConverged = true;
                for (int i = 0; i < n; i++)
                {
                    if (Math.Abs(x[i] - prevX[i]) > tolerance)
                    {
                        isConverged = false;
                        break;
                    }
                }

                steps.AppendLine();

                if (isConverged)
                {
                    steps.AppendLine("✅ Converged after " + iter + " iterations:");
                    for (int i = 0; i < n; i++)
                        steps.AppendLine($"{variableNames[i]} = {x[i]:F4}");
                    return steps.ToString();
                }
            }

            steps.AppendLine("────୨ৎ──── Did not converge within the maximum number of iterations ────୨ৎ────");
            for (int i = 0; i < n; i++)
                steps.AppendLine($"{variableNames[i]} ≈ {x[i]:F4} (last approximation)");

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
            UpdateGuessVisibility();
        }

        private void Button_Back_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }
}