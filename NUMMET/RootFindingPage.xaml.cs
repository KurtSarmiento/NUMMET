using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using NCalc;

namespace NUMMET
{
    public sealed partial class RootFindingPage : Page
    {
        public RootFindingPage()
        {
            this.InitializeComponent();
            Button_Submit.Click += Button_Submit_Click;
        }

        private void Button_Submit_Click(object sender, RoutedEventArgs e)
        {
            if ((Methods.SelectedItem as ComboBoxItem)?.Content.ToString() == "Bisection")
            {
                RunBisectionMethod();
            }
            else
            {
                TextBlock_Solution.Text = "Only Bisection is implemented so far.";
            }
        }

        private void RunBisectionMethod()
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
                string output = "Iter\ta\t\tb\t\tc\t\tf(c)\t\tea (%)\n";

                do
                {
                    prevC = c;
                    c = (a + b) / 2;
                    fc = EvaluateFunction(fxString, c);
                    iteration++;

                    if (iteration > 1)
                        ea = Math.Abs((c - prevC) / c) * 100;

                    output += $"{iteration}\t{a:F6}\t{b:F6}\t{c:F6}\t{fc:F6}\t{ea:F2}\n";

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

                output += $"\nApproximate Root: {c:F6}";
                TextBlock_Solution.Text = output; // Now using TextBlock_Solution
            }
            catch (Exception ex)
            {
                TextBlock_Solution.Text = $"Error: {ex.Message}"; // Error handling
            }
        }


        private double EvaluateFunction(string expression, double x)
        {
            var e = new Expression(expression);
            e.Parameters["x"] = x;

            e.EvaluateFunction += (name, args) =>
            {
                if (name == "Pow" && args.Parameters.Length == 2)
                {
                    double baseVal = Convert.ToDouble(args.Parameters[0].Evaluate());
                    double exponent = Convert.ToDouble(args.Parameters[1].Evaluate());
                    args.Result = Math.Pow(baseVal, exponent);
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
