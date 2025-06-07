using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BlackjackGame.Client
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                PreloadCardTextures();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error preloading card textures: {ex.Message}");
            }
        }

        private void PreloadCardTextures()
        {
            LoadImage("/Assets/Cards/card_backs/card_back.png");

            string[] suits = { "hearts", "diamonds", "clubs", "spades" };
            string[] ranks = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "jack", "queen", "king", "ace" };

            foreach (var suit in suits)
            {
                foreach (var rank in ranks)
                {
                    LoadImage($"/Assets/Cards/{suit}/{suit}_{rank}.png");
                }
            }
        }

        private void LoadImage(string resourcePath)
        {
            try
            {
                var uri = new Uri(resourcePath, UriKind.Relative);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = uri;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
            }
            catch
            {
                // nah, drauf geschissen
            }
        }
    }
}