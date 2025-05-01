using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using NCalc;
using Microsoft.UI.Xaml.Documents;
using System.Threading.Tasks;

namespace NUMMET
{
    public sealed partial class RootFindingPage : Page
    {
        public RootFindingPage()
        {
            this.InitializeComponent();
            Button_Submit.Click += Button_Submit_Click;
        }

        private async void Button_Submit_Click(object sender, RoutedEventArgs e)
        {
            if ((Methods.SelectedItem as ComboBoxItem)?.Content.ToString() == "Bisection")
            {
                await RunBisectionMethod();
            }
            else
            {
                TextBlock_Solution.Text = "Only Bisection is implemented so far.";
            }
        }

        private async Task RunBisectionMethod()
        {
            try
            {
                string fxString = TextBox_Equation.Text;
                double a = double.Parse(TextBox_Guess1.Text);
                double b = double.Parse(TextBox_Guess2.Text);
                double es = double.Parse(TextBox_PercentError.Text); // expected error in percent
                double fa = EvaluateFunction(fxString, a);
                double fb = EvaluateFunction(fxString, b);

                if (fa * fb > 0)
                {
                    TextBlock_Solution.Text = "f(a) and f(b) must have opposite signs.";
                    return;
                }

                double c = 0, fc, prevC = 0, ea = 100;
                int iteration = 0;
                string output = String.Format("{0,-8} {1,-16} {2,-16} {3,-16} {4,-16} {5,-8}\n", "Iter", "a", "b", "c", "f(c)", "ea (%)");

                do
                {
                    prevC = c;
                    c = (a + b) / 2;
                    fc = EvaluateFunction(fxString, c);
                    iteration++;

                    if (iteration > 1)
                        ea = Math.Abs((c - prevC) / c) * 100;

                    output += String.Format("{0,-8} {1,-16:F4} {2,-16:F4} {3,-16:F4} {4,-16:F4} {5,-8:F2}\n", iteration, a, b, c, fc, ea);

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

                } while (ea > es);

                output += $"\nAPPROXIMATE ROOT: {c:F4}";

                await TypeOutSolution(output); // Call async method to type out the solution gradually
            }
            catch (Exception ex)
            {
                TextBlock_Solution.Text = $"Error: {ex.Message}"; // Error handling
            }
        }

        private async Task TypeOutSolution(string output)
        {
            TextBlock solutionBlock = TextBlock_Solution;
            solutionBlock.Inlines.Clear();

            string[] lines = output.Split('\n');
            foreach (string line in lines)
            {
                // Handle special formatting for "APPROXIMATE ROOT"
                if (line.StartsWith("APPROXIMATE ROOT:"))
                {
                    var labelRun = new Run { Text = "APPROXIMATE ROOT: " };
                    var boldRun = new Run
                    {
                        Text = line.Replace("APPROXIMATE ROOT: ", ""),
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold
                    };

                    solutionBlock.Inlines.Add(labelRun);
                    solutionBlock.Inlines.Add(boldRun);
                }
                else
                {
                    solutionBlock.Inlines.Add(new Run { Text = line });
                }
                solutionBlock.Inlines.Add(new LineBreak());

                // Wait a little before adding the next line (simulate typing effect)
                await Task.Delay(100); // Adjust delay as needed (in milliseconds)
            }
        }

        private double EvaluateFunction(string expression, double x)
        {
            var e = new Expression(expression);

            // Allow usage of "x" as the variable
            e.Parameters["x"] = x;

            // Handle custom or mapped functions
            e.EvaluateFunction += (name, args) =>
            {
                switch (name.ToLower())
                {
                    case "pow":
                        args.Result = Math.Pow(Convert.ToDouble(args.Parameters[0].Evaluate()), Convert.ToDouble(args.Parameters[1].Evaluate()));
                        break;
                    case "ln":
                        args.Result = Math.Log(Convert.ToDouble(args.Parameters[0].Evaluate()));
                        break;
                    case "log":
                        args.Result = Math.Log10(Convert.ToDouble(args.Parameters[0].Evaluate()));
                        break;
                    case "sin":
                        args.Result = Math.Sin(Convert.ToDouble(args.Parameters[0].Evaluate()));
                        break;
                    case "cos":
                        args.Result = Math.Cos(Convert.ToDouble(args.Parameters[0].Evaluate()));
                        break;
                    case "tan":
                        args.Result = Math.Tan(Convert.ToDouble(args.Parameters[0].Evaluate()));
                        break;
                    case "exp":
                        args.Result = Math.Exp(Convert.ToDouble(args.Parameters[0].Evaluate()));
                        break;
                }
            };

            // Add constant support for 'e'
            e.EvaluateParameter += (name, args) =>
            {
                if (name.ToLower() == "e")
                {
                    args.Result = Math.E;
                }
            };

            return Convert.ToDouble(e.Evaluate());
        }

        private void Button_Back_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }
}
