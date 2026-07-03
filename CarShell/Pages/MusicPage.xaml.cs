using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CarShell.Pages
{
    public partial class MusicPage : UserControl
    {
        public MusicPage(MainWindow mainWindow)
        {
            InitializeComponent();

            MusicWebView.Source = new Uri("https://music.youtube.com");
            ShowYoutubeMusic();
        }

        private void ResetTabs()
        {
            BtnYoutubeMusic.Background = System.Windows.Media.Brushes.Transparent;
            BtnRadioPlayer.Background = System.Windows.Media.Brushes.Transparent;
            BtnCarRadio.Background = System.Windows.Media.Brushes.Transparent;
        }

        private void HidePanels()
        {
            YoutubeMusicPanel.Visibility = Visibility.Collapsed;
            RadioPlayerPanel.Visibility = Visibility.Collapsed;
            CarRadioPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowYoutubeMusic()
        {
            HidePanels();
            ResetTabs();

            YoutubeMusicPanel.Visibility = Visibility.Visible;
            BtnYoutubeMusic.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0, 122, 204)
            );
        }

        private void ShowRadioPlayer()
        {
            HidePanels();
            ResetTabs();

            RadioPlayerPanel.Visibility = Visibility.Visible;
            BtnRadioPlayer.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0, 122, 204)
            );
        }

        private void ShowCarRadio()
        {
            HidePanels();
            ResetTabs();

            CarRadioPanel.Visibility = Visibility.Visible;
            BtnCarRadio.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0, 122, 204)
            );
        }

        private void YouTubeMusic_Click(object sender, RoutedEventArgs e)
        {
            ShowYoutubeMusic();
        }

        private void RadioPlayer_Click(object sender, RoutedEventArgs e)
        {
            ShowRadioPlayer();
        }

        private void CarRadio_Click(object sender, RoutedEventArgs e)
        {
            ShowCarRadio();
        }

        public async Task PlayPause()
        {
            await MusicWebView.ExecuteScriptAsync(
                "document.querySelector('.play-pause-button, button[aria-label*=Play], button[aria-label*=Pause]')?.click();"
            );
        }

        public async Task NextTrack()
        {
            await MusicWebView.ExecuteScriptAsync(
                "document.querySelector('.next-button, a.ytp-next-button')?.click();"
            );
        }

        public async Task PrevTrack()
        {
            await MusicWebView.ExecuteScriptAsync(
                "document.querySelector('.previous-button')?.click();"
            );
        }
    }
}