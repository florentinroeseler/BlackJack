// BlackjackGame.Client/MainWindow.xaml.cs
using System;
using System.Security.Claims;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BlackjackGame.Client
{
    public partial class MainWindow : Window
    {
        private const double CompactHeightThreshold = 550; // Höhe, unter der das kompakte Layout aktiviert wird

        public MainWindow()
        {
            InitializeComponent();
            SizeChanged += MainWindow_SizeChanged;
            // Initial Layout setzen
            UpdateLayoutState();
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateLayoutState();
        }

        private void UpdateLayoutState()
        {
            // Visual state basierend auf der Fensterhöhe wechseln
            if (ActualHeight < CompactHeightThreshold)
            {
                VisualStateManager.GoToState(this, "CompactLayout", true);
            }
            else
            {
                VisualStateManager.GoToState(this, "NormalLayout", true);
            }
        }
    }
}