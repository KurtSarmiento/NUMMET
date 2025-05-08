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
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }
        private void Button_RootFinding_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(RootFindingPage));
        }

        private void Button_EquationSolver_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(EquationSolverPage));
        }
    }
}
