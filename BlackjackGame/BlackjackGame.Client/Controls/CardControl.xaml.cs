using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace BlackjackGame.Client.Controls
{
    /// <summary>
    /// Interaktionslogik für CardControl.xaml
    /// </summary>
    public partial class CardControl : UserControl
    {
        public CardControl()
        {
            InitializeComponent();

            // Event-Handler für das Fehlereignis beim Laden des Bildes
            CardImage.ImageFailed += OnImageFailed;
        }

        private void OnImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            // Bei Ladeproblemen: Fallback auf Text-Darstellung
            CardText.Visibility = Visibility.Visible;
            CardImage.Visibility = Visibility.Collapsed;

            // Debug-Ausgabe
            System.Diagnostics.Debug.WriteLine($"Fehler beim Laden der Kartentextur: {e.ErrorException.Message}");
        }
    }
}