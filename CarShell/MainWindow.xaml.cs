using System.Windows;
using CarShell.Pages;

namespace CarShell
{
    public partial class MainWindow : Window
    {
        private HomePage homePage;
        private MusicPage musicPage;
        private NavigationPage navigationPage;
        private CameraPage cameraPage;
        private SettingsPage settingsPage;
        private BoardPage boardPage;
        private ErrorPage errorPage;

        public MainWindow()
        {
            InitializeComponent();

            homePage = new HomePage(this);
            musicPage = new MusicPage(this);
            navigationPage = new NavigationPage(this);
            cameraPage = new CameraPage(this);
            settingsPage = new SettingsPage(this);
            boardPage = new BoardPage(this);
            errorPage = new ErrorPage(this);
            ShowHome();
        }

        public void ShowHome()
        {
            MainContent.Content = homePage;
        }

        public void ShowMusic()
        {
            MainContent.Content = musicPage;
        }

        public void ShowNavigation()
        {
            MainContent.Content = navigationPage;
        }

        public void ShowCamera()
        {
            MainContent.Content = cameraPage;
        }

        public void ShowSettings()
        {
            MainContent.Content = settingsPage;
        }
        public void ShowBoard()
        {
            MainContent.Content = boardPage;
        }
        public void ShowError()
        {
            MainContent.Content = errorPage;
        }

        private void Home_Click(object sender, RoutedEventArgs e) => ShowHome();
        private void Navigation_Click(object sender, RoutedEventArgs e) => ShowNavigation();
        private void Music_Click(object sender, RoutedEventArgs e) => ShowMusic();
        private void Camera_Click(object sender, RoutedEventArgs e) => ShowCamera();
        private void Settings_Click(object sender, RoutedEventArgs e) => ShowSettings();
        private void Board_Click(object sender, RoutedEventArgs e) => ShowBoard();
        private void Error_Click(object sender, RoutedEventArgs e) => ShowError();
        private void CheckEngineButton_Click(object sender, RoutedEventArgs e)
        {
            ShowError();
        }

        private async void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            await musicPage.PlayPause();
        }

        private async void Next_Click(object sender, RoutedEventArgs e)
        {
            await musicPage.NextTrack();
        }

        private async void Prev_Click(object sender, RoutedEventArgs e)
        {
            await musicPage.PrevTrack();
        }
    }
}